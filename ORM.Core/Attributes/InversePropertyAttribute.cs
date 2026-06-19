namespace ORM.Core.Attributes;

/*
Označava suprotnu stranu navigacijske veze između dvije klase.
Vrijednost je ime navigacijskog svojstva na drugoj klasi.
*/
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class InversePropertyAttribute : Attribute
{
    public string Property { get; }

    public InversePropertyAttribute(string property)
    {
        Property = property;
    }
}
