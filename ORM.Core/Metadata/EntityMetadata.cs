namespace ORM.Core.Metadata;

public class EntityMetadata
{
    public Type EntityType { get; init; } = null!;

    /// <summary>Ime tablice u bazi (iz [Table] ili ime tipa).</summary>
    public string TableName { get; init; } = null!;

    public string? Schema { get; init; }

    /// <summary>Svi stupci (isključuje navigacijska svojstva i [NotMapped]).</summary>
    public IReadOnlyList<ColumnMetadata> Columns { get; init; } = [];

    /// <summary>Sva navigacijska svojstva (Reference i Collection).</summary>
    public IReadOnlyList<NavigationMetadata> Navigations { get; init; } = [];

    public ColumnMetadata PrimaryKey => Columns.Single(c => c.IsPrimaryKey);

    public string QualifiedTableName =>
        Schema is not null ? $"\"{Schema}\".\"{TableName}\"" : $"\"{TableName}\"";
}
