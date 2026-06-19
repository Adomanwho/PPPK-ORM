using ORM.Core.Attributes;

namespace ORM.Core.AttributeHandling.Handlers;

/*
Obrađuje [Key] — dodaje PRIMARY KEY fragment.
Identity (GENERATED ALWAYS AS IDENTITY) dodaje DatabaseGeneratedAttributeHandler.
*/
public class KeyAttributeHandler : IAttributeHandler
{
    public bool CanHandle(Attribute attribute) => attribute is KeyAttribute;

    public void Process(Attribute attribute, AttributeContext context)
    {
        context.ConstraintFragments.Add("PRIMARY KEY");
    }
}
