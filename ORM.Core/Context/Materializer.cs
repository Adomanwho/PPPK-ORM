using Npgsql;
using ORM.Core.Metadata;

namespace ORM.Core.Context;

/*
Čita retke iz NpgsqlDataReader-a i kreira instance entiteta putem refleksije.
Mapira vrijednosti stupaca na propertyje koristeći EntityMetadata.
*/
public static class Materializer
{
    public static List<T> Materialize<T>(NpgsqlDataReader reader, EntityMetadata metadata)
        where T : new()
    {
        var results = new List<T>();

        // Izgradimo brzi lookup: ime stupca u bazi → ColumnMetadata
        var colLookup = metadata.Columns.ToDictionary(
            c => c.ColumnName.ToLowerInvariant());

        while (reader.Read())
        {
            var entity = new T();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var fieldName = reader.GetName(i).ToLowerInvariant();

                if (!colLookup.TryGetValue(fieldName, out var col))
                    continue;

                var dbValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                SetValue(entity, col, dbValue);
            }

            results.Add(entity);
        }

        return results;
    }

    private static void SetValue(object entity, ColumnMetadata col, object? dbValue)
    {
        if (dbValue is null)
        {
            // Postavi null samo ako property prihvaća null
            if (!col.Property.PropertyType.IsValueType ||
                Nullable.GetUnderlyingType(col.Property.PropertyType) is not null)
            {
                col.Property.SetValue(entity, null);
            }
            return;
        }

        var targetType = Nullable.GetUnderlyingType(col.Property.PropertyType)
                         ?? col.Property.PropertyType;

        // Enum: baza vraća string (VARCHAR(50)), parsiramo ga natrag
        if (targetType.IsEnum)
        {
            var parsed = Enum.Parse(targetType, dbValue.ToString()!, ignoreCase: true);
            col.Property.SetValue(entity, parsed);
            return;
        }

        // Postgres driver vraća int64 za INT stupce u nekim slučajevima
        var converted = Convert.ChangeType(dbValue, targetType);
        col.Property.SetValue(entity, converted);
    }
}
