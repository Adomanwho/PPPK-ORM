using Npgsql;

namespace ORM.Core.Migrations;

/// <summary>
/// Konkretna Migration koja izvršava SQL stringove generirane od strane MigrationGenerator-a.
/// Ručno pisane migracije nasljeđuju apstraktnu Migration klasu direktno.
/// </summary>
public class GeneratedMigration : Migration
{
    private readonly string _name;
    private readonly IReadOnlyList<string> _upStatements;
    private readonly IReadOnlyList<string> _downStatements;

    public override string Name => _name;

    public GeneratedMigration(
        string name,
        IReadOnlyList<string> upStatements,
        IReadOnlyList<string> downStatements)
    {
        _name           = name;
        _upStatements   = upStatements;
        _downStatements = downStatements;
    }

    public override void Up(NpgsqlConnection connection)
    {
        foreach (var sql in _upStatements)
            Execute(connection, sql);
    }

    public override void Down(NpgsqlConnection connection)
    {
        foreach (var sql in _downStatements)
            Execute(connection, sql);
    }

    /// <summary>Ispisuje SQL koji će se izvršiti — korisno za pregled prije primjene.</summary>
    public void Preview()
    {
        Console.WriteLine($"=== Migration: {_name} ===");
        Console.WriteLine("-- UP --");
        foreach (var sql in _upStatements)
            Console.WriteLine(sql);
        Console.WriteLine("-- DOWN --");
        foreach (var sql in _downStatements)
            Console.WriteLine(sql);
    }
}
