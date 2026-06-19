namespace ORM.Core.ChangeTracking;

public enum EntityState
{
    Detached,  // nije praćen — nije dohvaćen niti dodan kroz DbContext
    Unchanged, // praćen, nema promjena od zadnjeg SaveChanges
    Added,     // nov, INSERT će se izvršiti pri SaveChanges
    Modified,  // jedan ili više propertyja se promijenilo, UPDATE će se izvršiti pri SaveChanges
    Deleted    // označen za brisanje, DELETE će se izvršiti pri SaveChanges
}
