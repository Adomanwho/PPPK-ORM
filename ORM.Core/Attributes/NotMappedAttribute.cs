namespace ORM.Core.Attributes;

/// <summary>
/// Property označen ovim atributom ORM ignorira — ne mapira se ni na jedan stupac.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class NotMappedAttribute : Attribute { }
