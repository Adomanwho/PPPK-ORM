using ORM.Core.Attributes;

namespace ORM.Core.AttributeHandling.Handlers;

/// <summary>
/// Obrađuje [Default("sql_izraz")] — dodaje DEFAULT fragment s SQL izrazom.
/// </summary>
public class DefaultAttributeHandler : IAttributeHandler
{
    public bool CanHandle(Attribute attribute) => attribute is DefaultAttribute;

    public void Process(Attribute attribute, AttributeContext context)
    {
        var attr = (DefaultAttribute)attribute;
        context.ConstraintFragments.Add($"DEFAULT {attr.SqlExpression}");
    }
}
