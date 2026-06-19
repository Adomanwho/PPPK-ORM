using ORM.Core.Attributes;
using ORM.Core.Metadata;

namespace ORM.Core.AttributeHandling.Handlers;

/*
Obrađuje [ForeignKey] na navigacijskim svojstvima.
Sam FK constraint (REFERENCES tablica(stupac)) generira DDL generator
jer treba znati ciljnu tablicu — ovaj handler samo validira da FK property postoji.
*/
public class ForeignKeyAttributeHandler : IAttributeHandler
{
    public bool CanHandle(Attribute attribute) => attribute is ForeignKeyAttribute;

    public void Process(Attribute attribute, AttributeContext context)
    {
        var attr    = (ForeignKeyAttribute)attribute;
        var fkProp  = context.Property.DeclaringType?
            .GetProperty(attr.Name);

        if (fkProp is null)
            throw new InvalidOperationException(
                $"[ForeignKey(\"{attr.Name}\")] na '{context.Property.DeclaringType?.Name}.{context.Property.Name}' " +
                $"pokazuje na nepostojeći property '{attr.Name}'.");
    }
}
