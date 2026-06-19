using ORM.Core.Attributes;
using ORM.Core.TypeMapping;
using System.Collections.Concurrent;
using System.Reflection;

namespace ORM.Core.Metadata;

public static class MetadataCache
{
    private static readonly ConcurrentDictionary<Type, EntityMetadata> Cache = new();

    public static EntityMetadata Get<T>() => Get(typeof(T));

    public static EntityMetadata Get(Type type) =>
        Cache.GetOrAdd(type, BuildMetadata);

    private static EntityMetadata BuildMetadata(Type type)
    {
        var tableAttr = type.GetCustomAttribute<Attributes.TableAttribute>();
        var tableName = tableAttr?.Name ?? type.Name;
        var schema    = tableAttr?.Schema;

        var columns     = new List<ColumnMetadata>();
        var navigations = new List<NavigationMetadata>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetCustomAttribute<NotMappedAttribute>() is not null)
                continue;

            if (IsNavigationProperty(prop))
            {
                var nav = BuildNavigation(prop);
                if (nav is not null)
                    navigations.Add(nav);
                continue;
            }

            columns.Add(BuildColumn(prop));
        }

        // Konvencija: ako nema [Key], pokušaj property koji se zove "Id" ili "{TypeName}Id"
        if (!columns.Any(c => c.IsPrimaryKey))
        {
            var convention = columns.FirstOrDefault(c =>
                c.Property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                c.Property.Name.Equals(type.Name + "Id", StringComparison.OrdinalIgnoreCase));

            if (convention is not null)
            {
                var idx = columns.IndexOf(convention);
                columns[idx] = convention with { IsPrimaryKey = true, IsIdentity = true, IsRequired = true };
            }
        }

        return new EntityMetadata
        {
            EntityType  = type,
            TableName   = tableName,
            Schema      = schema,
            Columns     = columns.AsReadOnly(),
            Navigations = navigations.AsReadOnly()
        };
    }

    private static ColumnMetadata BuildColumn(PropertyInfo prop)
    {
        var keyAttr     = prop.GetCustomAttribute<Attributes.KeyAttribute>();
        var dbGenAttr   = prop.GetCustomAttribute<Attributes.DatabaseGeneratedAttribute>();
        var requiredAttr= prop.GetCustomAttribute<Attributes.RequiredAttribute>();
        var uniqueAttr  = prop.GetCustomAttribute<Attributes.UniqueAttribute>();
        var defaultAttr = prop.GetCustomAttribute<Attributes.DefaultAttribute>();
        var columnAttr  = prop.GetCustomAttribute<Attributes.ColumnAttribute>();

        var columnName = columnAttr?.Name ?? prop.Name;
        var sqlType    = TypeMapper.Resolve(prop);

        // Tip je nullable ako je Nullable<T> ili nullable reference type bez [Required]
        var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
        var isValueType    = prop.PropertyType.IsValueType;
        var isNullable     = underlyingType is not null ||
                             (!isValueType && requiredAttr is null && keyAttr is null);

        var isPk       = keyAttr is not null;
        var isIdentity = dbGenAttr?.Option == Attributes.DatabaseGeneratedOption.Identity;

        return new ColumnMetadata
        {
            Property           = prop,
            ColumnName         = columnName,
            SqlType            = sqlType,
            IsPrimaryKey       = isPk,
            IsIdentity         = isIdentity,
            IsRequired         = isPk || requiredAttr is not null,
            IsUnique           = uniqueAttr is not null,
            IsNullable         = !isPk && isNullable,
            DefaultSqlExpression = defaultAttr?.SqlExpression
        };
    }

    private static NavigationMetadata? BuildNavigation(PropertyInfo prop)
    {
        var fkAttr      = prop.GetCustomAttribute<Attributes.ForeignKeyAttribute>();
        var inverseAttr = prop.GetCustomAttribute<Attributes.InversePropertyAttribute>();

        Type targetType;
        NavigationKind kind;

        var propType = prop.PropertyType;

        // Kolekcija: ICollection<T>, IEnumerable<T>, List<T>
        var collectionElement = GetCollectionElementType(propType);
        if (collectionElement is not null)
        {
            kind       = NavigationKind.Collection;
            targetType = collectionElement;
        }
        else
        {
            kind       = NavigationKind.Reference;
            targetType = propType;
        }

        // FK ime: iz atributa ili konvencijom "{NazivPropertyja}Id"
        var fkName = fkAttr?.Name ?? (kind == NavigationKind.Reference
            ? prop.Name + "Id"
            : targetType.Name + "Id");

        return new NavigationMetadata
        {
            Property              = prop,
            TargetEntityType      = targetType,
            Kind                  = kind,
            ForeignKeyPropertyName = fkName,
            InversePropertyName   = inverseAttr?.Property
        };
    }

    /*
    Property je navigacijsko svojstvo ako mu tip nije scalar (nije u TypeMapper-u,
    nije enum, nije Nullable scalar) i nije string/byte[].
    */
    private static bool IsNavigationProperty(PropertyInfo prop)
    {
        var t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        if (t == typeof(string) || t == typeof(byte[]))
            return false;

        if (t.IsPrimitive || t.IsEnum)
            return false;

        if (t == typeof(decimal) || t == typeof(Guid) ||
            t == typeof(DateTime) || t == typeof(DateTimeOffset) ||
            t == typeof(DateOnly) || t == typeof(TimeOnly))
            return false;

        // ICollection<T>, IEnumerable<T>, List<T>, itd.
        if (GetCollectionElementType(prop.PropertyType) is not null)
            return true;

        // Klase koje nisu sistemske (entity reference)
        return t.IsClass && t.Namespace?.StartsWith("System") != true;
    }

    private static Type? GetCollectionElementType(Type type)
    {
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(ICollection<>) || def == typeof(IEnumerable<>) ||
                def == typeof(List<>) || def == typeof(IList<>) || def == typeof(HashSet<>))
                return type.GetGenericArguments()[0];
        }

        // Implementira li IEnumerable<T>?
        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elem = iface.GetGenericArguments()[0];
                if (elem != typeof(char)) // string implementira IEnumerable<char>
                    return elem;
            }
        }

        return null;
    }
}
