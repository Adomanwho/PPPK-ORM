namespace ORM.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TableAttribute : Attribute
{
    public string Name { get; }
    public string? Schema { get; set; }

    public TableAttribute(string name)
    {
        Name = name;
    }
}
