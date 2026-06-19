using ORM.Core.Metadata;
using System.Reflection;

namespace ORM.Core.AttributeHandling;

/*
Kontekst koji se predaje svakom handleru pri obradi jednog propertyja.
Handler čita atribute s Propertyja i upisuje rezultate u ColumnBuilder.
*/
public class AttributeContext
{
    public PropertyInfo Property { get; init; } = null!;
    public EntityMetadata EntityMetadata { get; init; } = null!;

    /*
    Akumulirani SQL fragmenti constraints-a za ovaj stupac (npr. "NOT NULL", "UNIQUE").
    Handleri dodaju fragmente u ovu listu; DDL generator ih spaja.
    */
    public List<string> ConstraintFragments { get; } = [];
}
