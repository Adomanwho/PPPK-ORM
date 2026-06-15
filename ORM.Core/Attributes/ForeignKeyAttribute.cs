namespace ORM.Core.Attributes;

/// <summary>
/// Stavlja se na navigacijsko svojstvo i označava koji property na istoj klasi
/// nosi vrijednost stranog ključa.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ForeignKeyAttribute : Attribute
{
    /// <summary>Ime propertyja koji sadrži FK vrijednost (npr. "LijecnikId").</summary>
    public string Name { get; }

    public ForeignKeyAttribute(string name)
    {
        Name = name;
    }
}
