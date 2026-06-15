using ORM.Core.Attributes;

namespace ORM.Core.AttributeHandling.Handlers;

/// <summary>
/// Obrađuje [Unique] — dodaje UNIQUE fragment.
/// </summary>
public class UniqueAttributeHandler : IAttributeHandler
{
    public bool CanHandle(Attribute attribute) => attribute is UniqueAttribute;

    public void Process(Attribute attribute, AttributeContext context)
    {
        context.ConstraintFragments.Add("UNIQUE");
    }
}
