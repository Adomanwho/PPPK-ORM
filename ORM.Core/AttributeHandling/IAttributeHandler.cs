namespace ORM.Core.AttributeHandling;

public interface IAttributeHandler
{
    // Vraća true ako ovaj handler zna obraditi dani atribut.
    bool CanHandle(Attribute attribute);

    // Primjenjuje logiku atributa i upisuje rezultate u kontekst.
    void Process(Attribute attribute, AttributeContext context);
}
