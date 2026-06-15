namespace ORM.Core.AttributeHandling;

public interface IAttributeHandler
{
    /// <summary>Vraća true ako ovaj handler zna obraditi dani atribut.</summary>
    bool CanHandle(Attribute attribute);

    /// <summary>Primjenjuje logiku atributa i upisuje rezultate u kontekst.</summary>
    void Process(Attribute attribute, AttributeContext context);
}
