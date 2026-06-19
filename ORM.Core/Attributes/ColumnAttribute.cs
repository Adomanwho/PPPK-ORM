namespace ORM.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ColumnAttribute : Attribute
{
    public string Name { get; }
    /*
    Opcionalno ručno zadavanje SQL tipa (npr. "VARCHAR(100)").
    Ako nije zadano, TypeMapper određuje tip automatski.
    */
    public string? TypeName { get; set; }
    public int Order { get; set; }

    public ColumnAttribute(string name)
    {
        Name = name;
    }
}
