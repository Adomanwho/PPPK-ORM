using System.Reflection;

namespace ORM.Core.Metadata;

public record class ColumnMetadata
{
    public PropertyInfo Property { get; init; } = null!;

    // Ime stupca u bazi (iz [Column] ili ime propertyja).
    public string ColumnName { get; init; } = null!;

    // SQL tip stupca (npr. "VARCHAR(100)", "INT", "TIMESTAMP WITHOUT TIME ZONE").
    public string SqlType { get; init; } = null!;

    public bool IsPrimaryKey { get; init; }
    public bool IsIdentity { get; init; }
    public bool IsRequired { get; init; }
    public bool IsUnique { get; init; }
    public bool IsNullable { get; init; }

    // SQL izraz za DEFAULT (npr. "NOW()", "'active'"). Null znači bez DEFAULT-a.
    public string? DefaultSqlExpression { get; init; }
}
