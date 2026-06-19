using System.Reflection;

namespace ORM.Core.Metadata;

public enum NavigationKind
{
    Reference,   // referenca na jedan entitet (npr. Lijecnik na PovijestBolesti)
    Collection   // kolekcija entiteta (npr. ICollection<PovijestBolesti> na Lijecnik)
}

public class NavigationMetadata
{
    public PropertyInfo Property { get; init; } = null!;

    // Tip entiteta na koji navigacija pokazuje.
    public Type TargetEntityType { get; init; } = null!;

    public NavigationKind Kind { get; init; }

    /*
    Ime FK propertyja na ovoj klasi (za Reference) ili na ciljnoj klasi (za Collection).
    Određuje se iz [ForeignKey] ili konvencijom (NazivTipa + "Id").
    */
    public string ForeignKeyPropertyName { get; init; } = null!;

    // Ime suprotnog navigacijskog propertyja (iz [InverseProperty]).
    public string? InversePropertyName { get; init; }
}
