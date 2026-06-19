using ORM.Core.Metadata;

namespace ORM.Core.ChangeTracking;

/*
Centralni registar praćenih entiteta unutar jednog DbContext-a.
Koristi referencijalni identitet objekta (RuntimeHelpers.GetHashCode) kao ključ
kako bi mogao pratiti isti objekt bez obzira na override od Equals/GetHashCode.
*/
public class ChangeTracker
{
    // Koristimo ConditionalWeakTable da ne držimo objekte živima ako ih nitko drugi ne referencira
    private readonly Dictionary<int, EntityEntry> _entries = [];

    // ── Praćenje ─────────────────────────────────────────────────────────────

    // Počni pratiti entitet kao Unchanged (dohvaćen iz baze).
    public EntityEntry Track(object entity, EntityMetadata metadata)
    {
        var key = GetKey(entity);
        if (_entries.TryGetValue(key, out var existing))
            return existing;

        var entry = new EntityEntry(entity, EntityState.Unchanged, metadata);
        _entries[key] = entry;
        return entry;
    }

    // Označi entitet kao Added (novi, čeka INSERT).
    public EntityEntry Add(object entity, EntityMetadata metadata)
    {
        var key   = GetKey(entity);
        var entry = new EntityEntry(entity, EntityState.Added, metadata);
        _entries[key] = entry;
        return entry;
    }

    // Označi praćeni entitet kao Deleted (čeka DELETE).
    public EntityEntry Remove(object entity, EntityMetadata metadata)
    {
        var key = GetKey(entity);

        if (_entries.TryGetValue(key, out var existing))
        {
            existing.State = EntityState.Deleted;
            return existing;
        }

        // Entitet nije još praćen — dodaj ga direktno kao Deleted
        var entry = new EntityEntry(entity, EntityState.Deleted, metadata);
        _entries[key] = entry;
        return entry;
    }

    // ── Detekcija promjena ───────────────────────────────────────────────────

    /*
    Prolazi sve Unchanged entitete i uspoređuje snapshot s trenutnim vrijednostima.
    Entitete koji imaju promjene prebacuje u Modified.
    */
    public void DetectChanges()
    {
        foreach (var entry in _entries.Values)
            entry.DetectChanges();
    }

    // ── Upiti nad stanjima ───────────────────────────────────────────────────

    public IEnumerable<EntityEntry> GetEntries(EntityState state) =>
        _entries.Values.Where(e => e.State == state);

    public IEnumerable<EntityEntry> GetAllEntries() =>
        _entries.Values;

    public EntityEntry? FindEntry(object entity) =>
        _entries.TryGetValue(GetKey(entity), out var entry) ? entry : null;

    // ── Nakon SaveChanges ────────────────────────────────────────────────────

    /*
    Nakon uspješnog SaveChanges: Added → Unchanged (s novim snapshotom),
    Modified → Unchanged (s novim snapshotom), Deleted → ukloni iz trackera.
    */
    public void AcceptChanges()
    {
        var toRemove = new List<int>();

        foreach (var (key, entry) in _entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                case EntityState.Modified:
                    entry.State = EntityState.Unchanged;
                    entry.TakeSnapshot();
                    break;

                case EntityState.Deleted:
                    toRemove.Add(key);
                    break;
            }
        }

        foreach (var key in toRemove)
            _entries.Remove(key);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private static int GetKey(object entity) =>
        System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(entity);
}
