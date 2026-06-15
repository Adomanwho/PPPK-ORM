using System.Reflection;

namespace ORM.Core.Metadata;

public record class ColumnMetadata
{
    public PropertyInfo Property { get; init; } = null!;

    /// <summary>Ime stupca u bazi (iz [Column] ili ime propertyja).</summary>
    public string ColumnName { get; init; } = null!;

    /// <summary>SQL tip stupca (npr. "VARCHAR(100)", "INT", "TIMESTAMP WITHOUT TIME ZONE").</summary>
    public string SqlType { get; init; } = null!;

    public bool IsPrimaryKey { get; init; }
    public bool IsIdentity { get; init; }
    public bool IsRequired { get; init; }
    public bool IsUnique { get; init; }
    public bool IsNullable { get; init; }

    /// <summary>SQL izraz za DEFAULT (npr. "NOW()", "'active'"). Null znači bez DEFAULT-a.</summary>
    public string? DefaultSqlExpression { get; init; }
}
