namespace ORM.Core.Attributes;

/*
Označava NOT NULL constraint na stupcu.
Koristiti zajedno s "= null!;" za reference tipove koji su obavezni,
ili izostaviti (= null;) za nullable stupce.
*/
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class RequiredAttribute : Attribute { }
