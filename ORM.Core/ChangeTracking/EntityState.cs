namespace ORM.Core.ChangeTracking;

public enum EntityState
{
    /// <summary>Entitet nije praćen — nije dohvaćen niti dodan kroz DbContext.</summary>
    Detached,
    /// <summary>Entitet je praćen, nema promjena od zadnjeg SaveChanges.</summary>
    Unchanged,
    /// <summary>Entitet je nov, INSERT će se izvršiti pri SaveChanges.</summary>
    Added,
    /// <summary>Jedan ili više propertyja se promijenilo, UPDATE će se izvršiti pri SaveChanges.</summary>
    Modified,
    /// <summary>Entitet je označen za brisanje, DELETE će se izvršiti pri SaveChanges.</summary>
    Deleted
}
