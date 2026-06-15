using ORM.Core.Metadata;
using System.Reflection;

namespace ORM.Core.AttributeHandling;

/// <summary>
/// Kontekst koji se predaje svakom handleru pri obradi jednog propertyja.
/// Handler čita atribute s Propertyja i upisuje rezultate u ColumnBuilder.
/// </summary>
public class AttributeContext
{
    public PropertyInfo Property { get; init; } = null!;
    public EntityMetadata EntityMetadata { get; init; } = null!;

    /// <summary>
    /// Akumulirani SQL fragmenti constraints-a za ovaj stupac (npr. "NOT NULL", "UNIQUE").
    /// Handleri dodaju fragmente u ovu listu; DDL generator ih spaja.
    /// </summary>
    public List<string> ConstraintFragments { get; } = [];
}
