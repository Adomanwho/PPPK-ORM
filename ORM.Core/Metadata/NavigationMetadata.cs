using System.Reflection;

namespace ORM.Core.Metadata;

public enum NavigationKind
{
    /// <summary>Referenca na jedan entitet (npr. Lijecnik na PovijestBolesti).</summary>
    Reference,
    /// <summary>Kolekcija entiteta (npr. ICollection&lt;PovijestBolesti&gt; na Lijecnik).</summary>
    Collection
}

public class NavigationMetadata
{
    public PropertyInfo Property { get; init; } = null!;

    /// <summary>Tip entiteta na koji navigacija pokazuje.</summary>
    public Type TargetEntityType { get; init; } = null!;

    public NavigationKind Kind { get; init; }

    /// <summary>
    /// Ime FK propertyja na ovoj klasi (za Reference) ili na ciljnoj klasi (za Collection).
    /// Određuje se iz [ForeignKey] ili konvencijom (NazivTipa + "Id").
    /// </summary>
    public string ForeignKeyPropertyName { get; init; } = null!;

    /// <summary>Ime suprotnog navigacijskog propertyja (iz [InverseProperty]).</summary>
    public string? InversePropertyName { get; init; }
}
