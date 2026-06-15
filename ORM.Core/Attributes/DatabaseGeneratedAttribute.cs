namespace ORM.Core.Attributes;

public enum DatabaseGeneratedOption
{
    None,
    Identity,
    Computed
}

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class DatabaseGeneratedAttribute : Attribute
{
    public DatabaseGeneratedOption Option { get; }

    public DatabaseGeneratedAttribute(DatabaseGeneratedOption option)
    {
        Option = option;
    }
}
