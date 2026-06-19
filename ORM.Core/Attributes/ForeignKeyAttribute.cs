namespace ORM.Core.Attributes;

/*
Stavlja se na navigacijsko svojstvo i označava koji property na istoj klasi
nosi vrijednost stranog ključa.
*/
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ForeignKeyAttribute : Attribute
{
    // Ime propertyja koji sadrži FK vrijednost (npr. "LijecnikId").
    public string Name { get; }

    public ForeignKeyAttribute(string name)
    {
        Name = name;
    }
}
