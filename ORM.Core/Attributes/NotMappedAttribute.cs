namespace ORM.Core.Attributes;

// Property označen ovim atributom ORM ignorira — ne mapira se ni na jedan stupac.
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class NotMappedAttribute : Attribute { }
