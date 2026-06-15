namespace ORM.Core.Migrations;

/// <summary>
/// Predstavlja jedan red u "__Migrations" tablici.
/// Runner upisuje zapis pri svakom Up() i briše ga pri Down().
/// </summary>
public class MigrationHistoryEntry
{
    public int Id { get; set; }
    public string MigrationName { get; set; } = null!;
    public DateTime AppliedAt { get; set; }
}
