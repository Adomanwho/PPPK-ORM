using ORM.Core.Metadata;
using System.Reflection;

namespace ORM.Core.AttributeHandling;

/*
Prolazi sve propertyje entiteta, za svaki atribut pronalazi odgovarajući handler
i poziva Process. Rezultat je lista AttributeContext-a s popunjenim ConstraintFragments.
Koristi se pri DDL generiranju (migracije, EnsureCreated).
*/
public class AttributeProcessor
{
    private readonly IEnumerable<IAttributeHandler> _handlers;

    public AttributeProcessor(IEnumerable<IAttributeHandler> handlers)
    {
        _handlers = handlers;
    }

    /*
    Obrađuje sve propertyje danog entitetskog tipa.
    Vraća rječnik: ime propertyja → kontekst s constraint fragmentima.
    */
    public Dictionary<string, AttributeContext> Process(Type entityType, EntityMetadata metadata)
    {
        var result = new Dictionary<string, AttributeContext>();

        foreach (var prop in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var ctx = new AttributeContext
            {
                Property       = prop,
                EntityMetadata = metadata
            };

            foreach (var attr in prop.GetCustomAttributes())
            {
                foreach (var handler in _handlers)
                {
                    if (handler.CanHandle(attr))
                        handler.Process(attr, ctx);
                }
            }

            result[prop.Name] = ctx;
        }

        return result;
    }

    public Dictionary<string, AttributeContext> Process<T>(EntityMetadata metadata) =>
        Process(typeof(T), metadata);
}
