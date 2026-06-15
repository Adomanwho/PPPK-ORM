using ORM.Core.Metadata;
using System.Reflection;

namespace ORM.Core.ChangeTracking;

/// <summary>
/// Prati jedan entitet: drži referencu na objekt, njegovo trenutno stanje
/// i snapshot originalnih vrijednosti dohvaćenih iz baze.
/// DetectChanges() uspoređuje snapshot s trenutnim vrijednostima.
/// </summary>
public class EntityEntry
{
    public object Entity { get; }
    public EntityState State { get; internal set; }
    public EntityMetadata Metadata { get; }

    /// <summary>
    /// Snapshot vrijednosti u trenutku praćenja (nakon dohvata iz baze ili SaveChanges).
    /// Ključ je ime propertyja, vrijednost je kopija originalnog podatka.
    /// </summary>
    private Dictionary<string, object?> _originalValues = [];

    public EntityEntry(object entity, EntityState state, EntityMetadata metadata)
    {
        Entity   = entity;
        State    = state;
        Metadata = metadata;

        if (state == EntityState.Unchanged)
            TakeSnapshot();
    }

    /// <summary>Sprema trenutne vrijednosti kao novi snapshot (poziva se nakon SaveChanges).</summary>
    internal void TakeSnapshot()
    {
        _originalValues = Metadata.Columns
            .ToDictionary(
                col => col.Property.Name,
                col => col.Property.GetValue(Entity));
    }

    /// <summary>
    /// Uspoređuje snapshot s trenutnim vrijednostima.
    /// Ako postoji razlika, postavlja State na Modified.
    /// </summary>
    internal void DetectChanges()
    {
        if (State is EntityState.Added or EntityState.Deleted)
            return;

        foreach (var col in Metadata.Columns)
        {
            if (!_originalValues.TryGetValue(col.Property.Name, out var original))
                continue;

            var current = col.Property.GetValue(Entity);

            if (!Equals(original, current))
            {
                State = EntityState.Modified;
                return;
            }
        }
    }

    /// <summary>Imena propertyja čije se vrijednosti razlikuju od snapshota.</summary>
    public IEnumerable<string> GetModifiedProperties()
    {
        foreach (var col in Metadata.Columns)
        {
            if (_originalValues.TryGetValue(col.Property.Name, out var original))
            {
                var current = col.Property.GetValue(Entity);
                if (!Equals(original, current))
                    yield return col.Property.Name;
            }
        }
    }
}
