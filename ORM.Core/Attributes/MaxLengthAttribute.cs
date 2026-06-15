namespace ORM.Core.Attributes;

/// <summary>
/// Definira maksimalnu duljinu string stupca → VARCHAR(n).
/// Bez ovog atributa string se mapira na TEXT.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class MaxLengthAttribute : Attribute
{
    public int Length { get; }

    public MaxLengthAttribute(int length)
    {
        Length = length;
    }
}
