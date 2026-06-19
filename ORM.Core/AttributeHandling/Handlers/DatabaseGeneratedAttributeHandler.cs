using ORM.Core.Attributes;

namespace ORM.Core.AttributeHandling.Handlers;

/*
Obrađuje [DatabaseGenerated(Identity)] — dodaje GENERATED ALWAYS AS IDENTITY fragment.
Postgres koristi ovaj standard umjesto SERIAL-a od verzije 10.
*/
public class DatabaseGeneratedAttributeHandler : IAttributeHandler
{
    public bool CanHandle(Attribute attribute) => attribute is DatabaseGeneratedAttribute;

    public void Process(Attribute attribute, AttributeContext context)
    {
        var attr = (DatabaseGeneratedAttribute)attribute;

        if (attr.Option == DatabaseGeneratedOption.Identity)
            context.ConstraintFragments.Add("GENERATED ALWAYS AS IDENTITY");
    }
}
