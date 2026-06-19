namespace ORM.Core.Metadata;

public class EntityMetadata
{
    public Type EntityType { get; init; } = null!;

    // Ime tablice u bazi (iz [Table] ili ime tipa).
    public string TableName { get; init; } = null!;

    public string? Schema { get; init; }

    // Svi stupci (isključuje navigacijska svojstva i [NotMapped]).
    public IReadOnlyList<ColumnMetadata> Columns { get; init; } = [];

    // Sva navigacijska svojstva (Reference i Collection).
    public IReadOnlyList<NavigationMetadata> Navigations { get; init; } = [];

    public ColumnMetadata PrimaryKey => Columns.Single(c => c.IsPrimaryKey);

    public string QualifiedTableName =>
        Schema is not null ? $"\"{Schema}\".\"{TableName}\"" : $"\"{TableName}\"";
}
