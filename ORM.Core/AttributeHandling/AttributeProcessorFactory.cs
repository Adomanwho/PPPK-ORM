using ORM.Core.AttributeHandling.Handlers;

namespace ORM.Core.AttributeHandling;

public static class AttributeProcessorFactory
{
    /// <summary>
    /// Vraća AttributeProcessor s registriranim handlerima u ispravnom redoslijedu.
    /// Redoslijed je bitan — PRIMARY KEY mora doći prije NOT NULL jer RequiredHandler
    /// preskače PK stupce (Postgres PRIMARY KEY već implicira NOT NULL).
    /// </summary>
    public static AttributeProcessor Create() => new(
    [
        new KeyAttributeHandler(),
        new DatabaseGeneratedAttributeHandler(),
        new RequiredAttributeHandler(),
        new UniqueAttributeHandler(),
        new DefaultAttributeHandler(),
        new ForeignKeyAttributeHandler(),
    ]);
}
