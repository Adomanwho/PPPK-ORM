using Npgsql;
using ORM.Core.ChangeTracking;
using ORM.Core.Metadata;
using ORM.Core.Querying;
using System.Linq.Expressions;
using System.Text;

namespace ORM.Core.Context;

/// <summary>
/// Predstavlja skup entiteta tipa T u bazi.
/// Pruža CRUD operacije, filtriranje, sortiranje i eager loading.
/// Sve promjene prolaze kroz ChangeTracker koji generira SQL pri SaveChanges.
/// </summary>
public class DbSet<T> where T : class, new()
{
    private readonly DbContext _context;
    private readonly EntityMetadata _metadata;

    internal DbSet(DbContext context, EntityMetadata metadata)
    {
        _context  = context;
        _metadata = metadata;
    }

    // ── Dodavanje / brisanje ─────────────────────────────────────────────────

    public void Add(T entity) =>
        _context.ChangeTracker.Add(entity, _metadata);

    public void Remove(T entity) =>
        _context.ChangeTracker.Remove(entity, _metadata);

    // ── Dohvat po PK ────────────────────────────────────────────────────────

    public T? Find(object pkValue)
    {
        var pk  = _metadata.PrimaryKey;
        var sql = $"SELECT * FROM {_metadata.QualifiedTableName} WHERE \"{pk.ColumnName}\" = @pk LIMIT 1";

        List<T> results;
        using (var cmd = _context.CreateCommand(sql))
        {
            cmd.Parameters.AddWithValue("@pk", pkValue);
            using var reader = cmd.ExecuteReader();
            results = Materializer.Materialize<T>(reader, _metadata);
        } // reader i cmd zatvoreni — konekcija slobodna za sljedeći upit

        if (results.Count == 0) return null;

        var entity = results[0];
        _context.ChangeTracker.Track(entity, _metadata);
        return entity;
    }

    // ── Dohvat svih ─────────────────────────────────────────────────────────

    public List<T> ToList() => BuildAndExecute(new QueryBuilder<T>(_metadata));

    // ── Fluent query API ─────────────────────────────────────────────────────

    /// <summary>Vraća FluentQuery koji se može dalje ulančavati (Where, OrderBy, Include, ...).</summary>
    public FluentQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        new FluentQuery<T>(this, _metadata).Where(predicate);

    public FluentQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector) =>
        new FluentQuery<T>(this, _metadata).OrderBy(keySelector);

    public FluentQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector) =>
        new FluentQuery<T>(this, _metadata).OrderByDescending(keySelector);

    public FluentQuery<T> Include<TNav>(Expression<Func<T, TNav>> navigation) =>
        new FluentQuery<T>(this, _metadata).Include(navigation);

    public FluentQuery<T> Skip(int count) =>
        new FluentQuery<T>(this, _metadata).Skip(count);

    public FluentQuery<T> Take(int count) =>
        new FluentQuery<T>(this, _metadata).Take(count);

    // ── Izvršavanje upita ────────────────────────────────────────────────────

    internal List<T> BuildAndExecute(QueryBuilder<T> builder)
    {
        var query = builder.Build();
        List<T> entities;

        using (var cmd = _context.CreateCommand(query.Sql))
        {
            foreach (var p in query.Parameters)
                cmd.Parameters.Add(p);

            using var reader = cmd.ExecuteReader();
            entities = Materializer.Materialize<T>(reader, _metadata);
        } // reader i cmd zatvoreni — konekcija slobodna za eager loading upite

        foreach (var entity in entities)
            _context.ChangeTracker.Track(entity, _metadata);

        if (builder.Includes.Count > 0)
            LoadIncludes(entities, builder.Includes);

        return entities;
    }

    // ── Eager loading ────────────────────────────────────────────────────────

    private void LoadIncludes(List<T> entities, IReadOnlyList<string> includes)
    {
        if (entities.Count == 0) return;

        foreach (var navName in includes)
        {
            var navMeta = _metadata.Navigations
                .FirstOrDefault(n => n.Property.Name == navName);

            if (navMeta is null) continue;

            if (navMeta.Kind == NavigationKind.Reference)
                LoadReferenceNavigation(entities, navMeta);
            else
                LoadCollectionNavigation(entities, navMeta);
        }
    }

    /// <summary>
    /// Reference (npr. PovijestBolesti.Pacijent): dohvati jedan entitet po FK vrijednosti.
    /// SELECT * FROM "Pacijenti" WHERE "Id" IN (fk1, fk2, ...)
    /// </summary>
    private void LoadReferenceNavigation(List<T> entities, NavigationMetadata navMeta)
    {
        var fkProp = typeof(T).GetProperty(navMeta.ForeignKeyPropertyName);
        if (fkProp is null) return;

        var targetMeta = MetadataCache.Get(navMeta.TargetEntityType);
        var targetPk   = targetMeta.PrimaryKey;

        // Skupi sve unikatne FK vrijednosti
        var fkValues = entities
            .Select(e => fkProp.GetValue(e))
            .Where(v => v is not null)
            .Distinct()
            .ToList();

        if (fkValues.Count == 0) return;

        var inParams = fkValues.Select((_, i) => $"@fk{i}").ToList();
        var sql = $"SELECT * FROM {targetMeta.QualifiedTableName} " +
                  $"WHERE \"{targetPk.ColumnName}\" IN ({string.Join(", ", inParams)})";

        using var cmd = _context.CreateCommand(sql);
        for (int i = 0; i < fkValues.Count; i++)
            cmd.Parameters.AddWithValue($"@fk{i}", fkValues[i]!);

        using var reader = cmd.ExecuteReader();
        var related = MaterializeUntyped(reader, targetMeta);

        // Rječnik: PK vrijednost → objekt
        var relatedByPk = related.ToDictionary(
            r => targetPk.Property.GetValue(r)!);

        // Postavi navigacijski property na svakom entitetu
        foreach (var entity in entities)
        {
            var fkValue = fkProp.GetValue(entity);
            if (fkValue is not null && relatedByPk.TryGetValue(fkValue, out var relatedEntity))
                navMeta.Property.SetValue(entity, relatedEntity);
        }
    }

    /// <summary>
    /// Collection (npr. Pacijent.PovijestBolesti): dohvati sve retke s matching FK.
    /// SELECT * FROM "PovijestBolesti" WHERE "PacijentId" IN (id1, id2, ...)
    /// </summary>
    private void LoadCollectionNavigation(List<T> entities, NavigationMetadata navMeta)
    {
        var thisPk = _metadata.PrimaryKey;

        var pkValues = entities
            .Select(e => thisPk.Property.GetValue(e))
            .Where(v => v is not null)
            .Distinct()
            .ToList();

        if (pkValues.Count == 0) return;

        var targetMeta = MetadataCache.Get(navMeta.TargetEntityType);

        // FK stupac na ciljnoj tablici
        var fkColMeta = targetMeta.Columns
            .FirstOrDefault(c => c.Property.Name == navMeta.ForeignKeyPropertyName);
        var fkColName = fkColMeta?.ColumnName ?? navMeta.ForeignKeyPropertyName;

        var inParams = pkValues.Select((_, i) => $"@pk{i}").ToList();
        var sql = $"SELECT * FROM {targetMeta.QualifiedTableName} " +
                  $"WHERE \"{fkColName}\" IN ({string.Join(", ", inParams)})";

        using var cmd = _context.CreateCommand(sql);
        for (int i = 0; i < pkValues.Count; i++)
            cmd.Parameters.AddWithValue($"@pk{i}", pkValues[i]!);

        using var reader = cmd.ExecuteReader();
        var related = MaterializeUntyped(reader, targetMeta);

        // Grupiraj po FK vrijednosti
        var fkPropOnTarget = navMeta.TargetEntityType.GetProperty(navMeta.ForeignKeyPropertyName);
        if (fkPropOnTarget is null) return;

        var grouped = related
            .GroupBy(r => fkPropOnTarget.GetValue(r)!)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Postavi kolekciju na svakom parent entitetu
        var listType = typeof(List<>).MakeGenericType(navMeta.TargetEntityType);

        foreach (var entity in entities)
        {
            var pkValue = thisPk.Property.GetValue(entity);
            if (pkValue is null) continue;

            var collection = grouped.TryGetValue(pkValue, out var items)
                ? items
                : [];

            // Kreiramo List<TargetType> i punimo ga
            var typedList = Activator.CreateInstance(listType)!;
            var addMethod = listType.GetMethod("Add")!;
            foreach (var item in collection)
                addMethod.Invoke(typedList, [item]);

            navMeta.Property.SetValue(entity, typedList);
        }
    }

    /// <summary>Materijalizira nepoznati tip (za eager loading related entiteta).</summary>
    private static List<object> MaterializeUntyped(NpgsqlDataReader reader, EntityMetadata meta)
    {
        var results   = new List<object>();
        var colLookup = meta.Columns.ToDictionary(c => c.ColumnName.ToLowerInvariant());

        while (reader.Read())
        {
            var entity = Activator.CreateInstance(meta.EntityType)!;

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var fieldName = reader.GetName(i).ToLowerInvariant();
                if (!colLookup.TryGetValue(fieldName, out var col)) continue;

                var dbValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                if (dbValue is null) continue;

                var targetType = Nullable.GetUnderlyingType(col.Property.PropertyType)
                                 ?? col.Property.PropertyType;

                object converted = targetType.IsEnum
                    ? Enum.Parse(targetType, dbValue.ToString()!, ignoreCase: true)
                    : Convert.ChangeType(dbValue, targetType);

                col.Property.SetValue(entity, converted);
            }

            results.Add(entity);
        }

        return results;
    }
}
