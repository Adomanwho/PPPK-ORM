namespace ORM.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class DefaultAttribute : Attribute
{
    /// <summary>
    /// SQL izraz koji se upisuje direktno u DEFAULT klauzulu (npr. "NOW()", "'active'", "0").
    /// </summary>
    public string SqlExpression { get; }

    public DefaultAttribute(string sqlExpression)
    {
        SqlExpression = sqlExpression;
    }
}
