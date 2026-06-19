namespace ORM.Core.Attributes;

/*
Definira maksimalnu duljinu string stupca → VARCHAR(n).
Bez ovog atributa string se mapira na TEXT.
*/
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class MaxLengthAttribute : Attribute
{
    public int Length { get; }

    public MaxLengthAttribute(int length)
    {
        Length = length;
    }
}
