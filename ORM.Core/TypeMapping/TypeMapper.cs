using ORM.Core.Attributes;
using System.Reflection;

namespace ORM.Core.TypeMapping;

public static class TypeMapper
{
    private static readonly Dictionary<Type, string> Map = new()
    {
        [typeof(int)]            = "INT",
        [typeof(long)]           = "BIGINT",
        [typeof(short)]          = "SMALLINT",
        [typeof(decimal)]        = "DECIMAL",
        [typeof(float)]          = "FLOAT",
        [typeof(double)]         = "FLOAT",
        [typeof(bool)]           = "BOOLEAN",
        [typeof(char)]           = "CHAR(1)",
        [typeof(string)]         = "TEXT",
        [typeof(Guid)]           = "UUID",
        [typeof(DateTime)]       = "TIMESTAMP WITHOUT TIME ZONE",
        [typeof(DateTimeOffset)] = "TIMESTAMP WITH TIME ZONE",
        [typeof(DateOnly)]       = "DATE",
        [typeof(TimeOnly)]       = "TIME WITHOUT TIME ZONE",
        [typeof(byte[])]         = "BYTEA",
    };

    /// <summary>
    /// Određuje SQL tip za dani property.
    /// Poštuje redoslijed: [Column(TypeName)] → [MaxLength] → enum → Nullable → Map.
    /// </summary>
    public static string Resolve(PropertyInfo property)
    {
        var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
        if (columnAttr?.TypeName is not null)
            return columnAttr.TypeName;

        var clrType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (clrType.IsEnum)
            return "VARCHAR(50)";

        if (clrType == typeof(string))
        {
            var maxLen = property.GetCustomAttribute<MaxLengthAttribute>();
            return maxLen is not null ? $"VARCHAR({maxLen.Length})" : "TEXT";
        }

        if (Map.TryGetValue(clrType, out var sqlType))
            return sqlType;

        throw new NotSupportedException(
            $"Tip '{clrType.Name}' na propertyju '{property.DeclaringType?.Name}.{property.Name}' " +
            $"nije podržan. Koristite [Column(TypeName = \"...\")] za ručno zadavanje SQL tipa.");
    }
}
