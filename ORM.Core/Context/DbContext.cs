using Npgsql;
using ORM.Core.AttributeHandling;
using ORM.Core.ChangeTracking;
using ORM.Core.Metadata;
using System.Reflection;
using System.Text;

namespace ORM.Core.Context;

/// <summary>
/// Apstraktna baza svakog konkretnog konteksta.
/// Upravlja konekcijom, ChangeTracker-om i SaveChanges logikom.
/// Konkretna klasa deklarira DbSet&lt;T&gt; propertyje koji se inicijaliziraju
/// automatski u konstruktoru putem refleksije.
/// </summary>
public abstract class DbContext : IDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly AttributeProcessor _attributeProcessor;
    private bool _disposed;

    public ChangeTracker ChangeTracker { get; } = new();

    protected DbContext(string connectionString)
    {
        _connection         = new NpgsqlConnection(connectionString);
        _attributeProcessor = AttributeProcessorFactory.Create();
        _connection.Open();
        InitializeDbSets();
    }

    // ── DbSet inicijalizacija ────────────────────────────────────────────────

    /// <summary>
    /// Putem refleksije pronalazi sve DbSet&lt;T&gt; propertyje na konkretnoj klasi
    /// i kreira instance s ispravnim metapodacima.
    /// </summary>
    private void InitializeDbSets()
    {
        var dbSetType = typeof(DbSet<>);

        foreach (var prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.PropertyType.IsGenericType) continue;
            if (prop.PropertyType.GetGenericTypeDefinition() != dbSetType) continue;

            var entityType = prop.PropertyType.GetGenericArguments()[0];
            var metadata   = MetadataCache.Get(entityType);
            var dbSet      = Activator.CreateInstance(
                                 dbSetType.MakeGenericType(entityType),
                                 BindingFlags.NonPublic | BindingFlags.Instance,
                                 null,
                                 [this, metadata],
                                 null)!;

            prop.SetValue(this, dbSet);
        }
    }

    // ── Connection management ────────────────────────────────────────────────

    internal NpgsqlCommand CreateCommand(string sql)
    {
        return new NpgsqlCommand(sql, _connection);
    }

    // ── SaveChanges ──────────────────────────────────────────────────────────

    /// <summary>
    /// Detektira sve promjene, generira i izvršava INSERT/UPDATE/DELETE SQL,
    /// zatim poziva AcceptChanges() da resetira tracker.
    /// Sve se izvršava unutar jedne transakcije.
    /// </summary>
    public int SaveChanges()
    {
        ChangeTracker.DetectChanges();

        using var transaction = _connection.BeginTransaction();
        int affected = 0;

        try
        {
            affected += ProcessAdded(transaction);
            affected += ProcessModified(transaction);
            affected += ProcessDeleted(transaction);

            transaction.Commit();
            ChangeTracker.AcceptChanges();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return affected;
    }

    private int ProcessAdded(NpgsqlTransaction transaction)
    {
        int count = 0;

        foreach (var entry in ChangeTracker.GetEntries(EntityState.Added))
        {
            var meta    = entry.Metadata;
            var pk      = meta.PrimaryKey;

            // Stupci za INSERT: preskočiti identity PK (baza ga generira)
            var insertCols = meta.Columns
                .Where(c => !(c.IsPrimaryKey && c.IsIdentity))
                .ToList();

            var colNames  = insertCols.Select(c => $"\"{c.ColumnName}\"");
            var paramNames = insertCols.Select((c, i) => $"@ins{i}");

            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {meta.QualifiedTableName} ");
            sql.Append($"({string.Join(", ", colNames)}) ");
            sql.Append($"VALUES ({string.Join(", ", paramNames)})");

            // RETURNING vraća generirani PK da ga možemo upisati natrag na objekt
            if (pk.IsIdentity)
                sql.Append($" RETURNING \"{pk.ColumnName}\"");

            using var cmd = new NpgsqlCommand(sql.ToString(), _connection, transaction);

            for (int i = 0; i < insertCols.Count; i++)
            {
                var col   = insertCols[i];
                var value = col.Property.GetValue(entry.Entity) ?? DBNull.Value;

                // Enum: pohrani kao string
                if (value is Enum e)
                    value = e.ToString();

                cmd.Parameters.AddWithValue($"@ins{i}", value);
            }

            if (pk.IsIdentity)
            {
                var generatedId = cmd.ExecuteScalar();
                pk.Property.SetValue(entry.Entity, Convert.ChangeType(generatedId, pk.Property.PropertyType));
            }
            else
            {
                cmd.ExecuteNonQuery();
            }

            count++;
        }

        return count;
    }

    private int ProcessModified(NpgsqlTransaction transaction)
    {
        int count = 0;

        foreach (var entry in ChangeTracker.GetEntries(EntityState.Modified))
        {
            var meta           = entry.Metadata;
            var pk             = meta.PrimaryKey;
            var modifiedProps  = entry.GetModifiedProperties().ToList();

            if (modifiedProps.Count == 0) continue;

            var setClauses = modifiedProps
                .Select((name, i) => $"\"{meta.Columns.First(c => c.Property.Name == name).ColumnName}\" = @upd{i}")
                .ToList();

            var sql = $"UPDATE {meta.QualifiedTableName} " +
                      $"SET {string.Join(", ", setClauses)} " +
                      $"WHERE \"{pk.ColumnName}\" = @pk";

            using var cmd = new NpgsqlCommand(sql, _connection, transaction);

            for (int i = 0; i < modifiedProps.Count; i++)
            {
                var prop  = meta.EntityType.GetProperty(modifiedProps[i])!;
                var value = prop.GetValue(entry.Entity) ?? DBNull.Value;

                if (value is Enum e)
                    value = e.ToString();

                cmd.Parameters.AddWithValue($"@upd{i}", value);
            }

            cmd.Parameters.AddWithValue("@pk", pk.Property.GetValue(entry.Entity)!);
            count += cmd.ExecuteNonQuery();
        }

        return count;
    }

    private int ProcessDeleted(NpgsqlTransaction transaction)
    {
        int count = 0;

        foreach (var entry in ChangeTracker.GetEntries(EntityState.Deleted))
        {
            var meta = entry.Metadata;
            var pk   = meta.PrimaryKey;

            var sql = $"DELETE FROM {meta.QualifiedTableName} " +
                      $"WHERE \"{pk.ColumnName}\" = @pk";

            using var cmd = new NpgsqlCommand(sql, _connection, transaction);
            cmd.Parameters.AddWithValue("@pk", pk.Property.GetValue(entry.Entity)!);
            count += cmd.ExecuteNonQuery();
        }

        return count;
    }

    // ── EnsureCreated ────────────────────────────────────────────────────────

    /// <summary>
    /// Kreira tablice za sve DbSet&lt;T&gt; propertyje ako još ne postoje.
    /// Koristi AttributeProcessor za generiranje constraint fragmenata.
    /// </summary>
    public void EnsureCreated()
    {
        var dbSetType = typeof(DbSet<>);

        foreach (var prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.PropertyType.IsGenericType) continue;
            if (prop.PropertyType.GetGenericTypeDefinition() != dbSetType) continue;

            var entityType = prop.PropertyType.GetGenericArguments()[0];
            var metadata   = MetadataCache.Get(entityType);
            CreateTableIfNotExists(metadata);
        }

        // FK constraints dodajemo u drugom prolazu — sve tablice moraju već postojati
        foreach (var prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.PropertyType.IsGenericType) continue;
            if (prop.PropertyType.GetGenericTypeDefinition() != dbSetType) continue;

            var entityType = prop.PropertyType.GetGenericArguments()[0];
            var metadata   = MetadataCache.Get(entityType);
            AddForeignKeyConstraints(metadata);
        }
    }

    private void CreateTableIfNotExists(EntityMetadata metadata)
    {
        var contexts  = _attributeProcessor.Process(metadata.EntityType, metadata);
        var colDefs   = new List<string>();

        foreach (var col in metadata.Columns)
        {
            var sb = new StringBuilder();
            sb.Append($"    \"{col.ColumnName}\" {col.SqlType}");

            if (contexts.TryGetValue(col.Property.Name, out var ctx) &&
                ctx.ConstraintFragments.Count > 0)
            {
                sb.Append($" {string.Join(" ", ctx.ConstraintFragments)}");
            }
            else if (!col.IsNullable && !col.IsPrimaryKey)
            {
                sb.Append(" NOT NULL");
            }

            colDefs.Add(sb.ToString());
        }

        var sql = $"CREATE TABLE IF NOT EXISTS {metadata.QualifiedTableName} (\n" +
                  string.Join(",\n", colDefs) +
                  "\n);";

        using var cmd = CreateCommand(sql);
        cmd.ExecuteNonQuery();
    }

    private void AddForeignKeyConstraints(EntityMetadata metadata)
    {
        foreach (var nav in metadata.Navigations.Where(n => n.Kind == NavigationKind.Reference))
        {
            var fkCol = metadata.Columns
                .FirstOrDefault(c => c.Property.Name == nav.ForeignKeyPropertyName);

            if (fkCol is null) continue;

            var targetMeta = MetadataCache.Get(nav.TargetEntityType);
            var constraintName = $"FK_{metadata.TableName}_{fkCol.ColumnName}";

            // Dodaj FK samo ako još ne postoji
            var checkSql = $"""
                SELECT COUNT(*) FROM information_schema.table_constraints
                WHERE constraint_name = '{constraintName}'
                AND table_name = '{metadata.TableName}';
                """;

            using var checkCmd = CreateCommand(checkSql);
            var exists = Convert.ToInt64(checkCmd.ExecuteScalar()) > 0;
            if (exists) continue;

            var fkSql = $"""
                ALTER TABLE {metadata.QualifiedTableName}
                ADD CONSTRAINT "{constraintName}"
                FOREIGN KEY ("{fkCol.ColumnName}")
                REFERENCES {targetMeta.QualifiedTableName} ("{targetMeta.PrimaryKey.ColumnName}");
                """;

            using var fkCmd = CreateCommand(fkSql);
            fkCmd.ExecuteNonQuery();
        }
    }

    // ── Dispose ──────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _connection.Close();
        _connection.Dispose();
        _disposed = true;
    }
}
