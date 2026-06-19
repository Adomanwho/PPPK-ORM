using Npgsql;
using ORM.Core.AttributeHandling;
using ORM.Core.Metadata;
using System.Text;

namespace ORM.Core.Migrations;

/*
Uspoređuje EntityMetadata s trenutnim stanjem baze (SchemaInspector)
i generira konkretnu Migration instancu s Up/Down SQL-om.
Pretpostavke radi jednostavnosti:
  - preimenovanje stupaca se ne detektira (tretira se kao drop + add)
  - promjena PK se ne podržava
*/
public class MigrationGenerator
{
    private readonly SchemaInspector _inspector;
    private readonly AttributeProcessor _attributeProcessor;

    public MigrationGenerator(NpgsqlConnection connection)
    {
        _inspector          = new SchemaInspector(connection);
        _attributeProcessor = AttributeProcessorFactory.Create();
    }

    /*
    Generira Migration za razliku između zadanog skupa entiteta i baze.
    Vraća null ako nema razlika.
    */
    public GeneratedMigration? GenerateDiff(IEnumerable<Type> entityTypes, string migrationName)
    {
        var existingTables  = _inspector.GetTableNames()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingColumns = _inspector.GetAllColumns();

        var upStatements   = new List<string>();
        var downStatements = new List<string>();

        foreach (var entityType in entityTypes)
        {
            var meta      = MetadataCache.Get(entityType);
            var tableName = meta.TableName;

            if (!existingTables.Contains(tableName))
            {
                // Tablica ne postoji → CREATE TABLE
                upStatements.Add(BuildCreateTable(meta));
                downStatements.Add($"DROP TABLE IF EXISTS {meta.QualifiedTableName};");
            }
            else
            {
                // Tablica postoji → provjeri razlike u stupcima
                var dbCols = existingColumns.TryGetValue(tableName, out var cols)
                    ? cols.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, DbColumnInfo>();

                foreach (var col in meta.Columns)
                {
                    if (!dbCols.ContainsKey(col.ColumnName))
                    {
                        // Novi stupac → ADD COLUMN
                        var colDef = BuildColumnDefinition(meta, col);
                        upStatements.Add(
                            $"ALTER TABLE {meta.QualifiedTableName} ADD COLUMN {colDef};");
                        downStatements.Add(
                            $"ALTER TABLE {meta.QualifiedTableName} DROP COLUMN IF EXISTS \"{col.ColumnName}\";");
                    }
                    else
                    {
                        // Stupac postoji — provjeri tip i nullable
                        var dbCol = dbCols[col.ColumnName];
                        var typeChanged    = !SqlTypesCompatible(col.SqlType, dbCol.DataType);
                        var nullableChanged = col.IsNullable != dbCol.IsNullable && !col.IsPrimaryKey;

                        if (typeChanged)
                        {
                            upStatements.Add(
                                $"ALTER TABLE {meta.QualifiedTableName} " +
                                $"ALTER COLUMN \"{col.ColumnName}\" TYPE {col.SqlType} " +
                                $"USING \"{col.ColumnName}\"::{col.SqlType};");
                            downStatements.Add(
                                $"ALTER TABLE {meta.QualifiedTableName} " +
                                $"ALTER COLUMN \"{col.ColumnName}\" TYPE {dbCol.DataType} " +
                                $"USING \"{col.ColumnName}\"::{dbCol.DataType};");
                        }

                        if (nullableChanged)
                        {
                            var upOp   = col.IsNullable ? "DROP NOT NULL" : "SET NOT NULL";
                            var downOp = dbCol.IsNullable ? "DROP NOT NULL" : "SET NOT NULL";
                            upStatements.Add(
                                $"ALTER TABLE {meta.QualifiedTableName} " +
                                $"ALTER COLUMN \"{col.ColumnName}\" {upOp};");
                            downStatements.Add(
                                $"ALTER TABLE {meta.QualifiedTableName} " +
                                $"ALTER COLUMN \"{col.ColumnName}\" {downOp};");
                        }
                    }
                }

                // Stupci koji su u bazi ali ne u entitetu → DROP COLUMN
                var entityColNames = meta.Columns
                    .Select(c => c.ColumnName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var dbCol in dbCols.Values)
                {
                    if (!entityColNames.Contains(dbCol.ColumnName))
                    {
                        upStatements.Add(
                            $"ALTER TABLE {meta.QualifiedTableName} " +
                            $"DROP COLUMN IF EXISTS \"{dbCol.ColumnName}\";");
                        // Down ne može pouzdano rekonstruirati tip → komentiramo
                        downStatements.Add(
                            $"-- Ručno dodati: ALTER TABLE {meta.QualifiedTableName} " +
                            $"ADD COLUMN \"{dbCol.ColumnName}\" {dbCol.DataType};");
                    }
                }
            }
        }

        if (upStatements.Count == 0)
            return null;

        // Down se izvršava obrnutim redoslijedom
        downStatements.Reverse();

        return new GeneratedMigration(migrationName, upStatements, downStatements);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string BuildCreateTable(EntityMetadata meta)
    {
        var contexts = _attributeProcessor.Process(meta.EntityType, meta);
        var colDefs  = new List<string>();

        foreach (var col in meta.Columns)
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

        return $"CREATE TABLE IF NOT EXISTS {meta.QualifiedTableName} (\n" +
               string.Join(",\n", colDefs) +
               "\n);";
    }

    private string BuildColumnDefinition(EntityMetadata meta, ColumnMetadata col)
    {
        var contexts = _attributeProcessor.Process(meta.EntityType, meta);
        var sb       = new StringBuilder();

        sb.Append($"\"{col.ColumnName}\" {col.SqlType}");

        if (contexts.TryGetValue(col.Property.Name, out var ctx) &&
            ctx.ConstraintFragments.Count > 0)
        {
            // Preskočiti PRIMARY KEY pri ADD COLUMN
            var fragments = ctx.ConstraintFragments
                .Where(f => !f.Contains("PRIMARY KEY"))
                .ToList();

            if (fragments.Count > 0)
                sb.Append($" {string.Join(" ", fragments)}");
        }
        else if (!col.IsNullable)
        {
            sb.Append(" NOT NULL");
        }

        return sb.ToString();
    }

    /*
    Grubo uspoređuje SQL tip iz metadataka s PostgreSQL tipom iz information_schema.
    information_schema vraća "character varying" za VARCHAR, "integer" za INT, itd.
    */
    private static bool SqlTypesCompatible(string metaType, string dbType)
    {
        var normalizedMeta = NormalizeSqlType(metaType);
        var normalizedDb   = NormalizeSqlType(dbType);
        return normalizedMeta.Equals(normalizedDb, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSqlType(string sqlType)
    {
        var t = sqlType.ToUpperInvariant().Trim();

        // information_schema nazivi → naši nazivi
        return t switch
        {
            "CHARACTER VARYING"               => "VARCHAR",
            "INTEGER"                         => "INT",
            "BIGINT"                          => "BIGINT",
            "SMALLINT"                        => "SMALLINT",
            "DOUBLE PRECISION"                => "FLOAT",
            "REAL"                            => "FLOAT",
            "BOOLEAN"                         => "BOOLEAN",
            "TEXT"                            => "TEXT",
            "UUID"                            => "UUID",
            "DATE"                            => "DATE",
            "BYTEA"                           => "BYTEA",
            "NUMERIC"                         => "DECIMAL",
            "TIMESTAMP WITHOUT TIME ZONE"     => "TIMESTAMP WITHOUT TIME ZONE",
            "TIMESTAMP WITH TIME ZONE"        => "TIMESTAMP WITH TIME ZONE",
            "TIME WITHOUT TIME ZONE"          => "TIME WITHOUT TIME ZONE",
            _ when t.StartsWith("VARCHAR")    => "VARCHAR",
            _ when t.StartsWith("CHAR(")      => "CHAR",
            _ when t.StartsWith("DECIMAL")    => "DECIMAL",
            _                                 => t
        };
    }
}
