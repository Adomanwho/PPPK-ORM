using ORM.Core.Attributes;

namespace ORM.Core.AttributeHandling.Handlers;

/*
Obrađuje [Required] — dodaje NOT NULL fragment.
PK stupci su implicitno NOT NULL pa ih ovaj handler preskoči
(PRIMARY KEY već implicira NOT NULL u PostgreSQL-u).
*/
public class RequiredAttributeHandler : IAttributeHandler
{
    public bool CanHandle(Attribute attribute) => attribute is RequiredAttribute;

    public void Process(Attribute attribute, AttributeContext context)
    {
        var col = context.EntityMetadata.Columns
            .FirstOrDefault(c => c.Property.Name == context.Property.Name);

        if (col is not null && col.IsPrimaryKey)
            return;

        context.ConstraintFragments.Add("NOT NULL");
    }
}
