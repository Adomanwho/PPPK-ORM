using Npgsql;
using System.Reflection;

namespace ORM.Core.Migrations;

/// <summary>
/// Izvršava migracije naprijed (Migrate) i unazad (Rollback).
/// Prati izvršene migracije u "__Migrations" tablici u bazi.
/// Migracije se sortiraju po imenu klase — konvencija: "YYYYMMDD_HHMMSS_Naziv".
/// Uz ručno pisane Migration podklase, runner može primiti i GeneratedMigration instance.
/// </summary>
public class MigrationRunner
{
    private readonly NpgsqlConnection _connection;

    public MigrationRunner(NpgsqlConnection connection)
    {
        _connection = connection;
        EnsureHistoryTable();
    }

    // ── Migrate (naprijed) ───────────────────────────────────────────────────

    /// <summary>
    /// Pronalazi sve Migration podklase u danom assembly-ju i izvršava one koje još nisu primijenjene.
    /// </summary>
    public void Migrate(Assembly assembly)
    {
        var migrations = DiscoverMigrations(assembly);
        var applied    = GetAppliedMigrations();

        var pending = migrations
            .Where(m => !applied.Contains(m.Name))
            .ToList();

        if (pending.Count == 0)
        {
            Console.WriteLine("Nema pending migracija.");
            return;
        }

        foreach (var migration in pending)
            ApplyUp(migration);
    }

    /// <summary>Izvršava jednu konkretnu migraciju naprijed (npr. GeneratedMigration).</summary>
    public void Migrate(Migration migration)
    {
        var applied = GetAppliedMigrations();

        if (applied.Contains(migration.Name))
        {
            Console.WriteLine($"Migracija '{migration.Name}' već je primijenjena.");
            return;
        }

        ApplyUp(migration);
    }

    // ── Rollback (unazad) ────────────────────────────────────────────────────

    /// <summary>Poništava zadnju primijenjenu migraciju.</summary>
    public void Rollback(Assembly assembly)
    {
        var applied = GetAppliedMigrations();
        if (applied.Count == 0)
        {
            Console.WriteLine("Nema primijenjenih migracija za rollback.");
            return;
        }

        var lastName  = applied.Last();
        var migration = DiscoverMigrations(assembly)
            .FirstOrDefault(m => m.Name == lastName);

        if (migration is null)
        {
            Console.WriteLine($"Migracija '{lastName}' nije pronađena u assembly-ju.");
            return;
        }

        ApplyDown(migration);
    }

    /// <summary>Poništava N zadnjih primijenjenih migracija.</summary>
    public void Rollback(Assembly assembly, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            var applied = GetAppliedMigrations();
            if (applied.Count == 0) break;

            var lastName  = applied.Last();
            var migration = DiscoverMigrations(assembly)
                .FirstOrDefault(m => m.Name == lastName);

            if (migration is null) break;
            ApplyDown(migration);
        }
    }

    // ── Status ───────────────────────────────────────────────────────────────

    public void PrintStatus(Assembly assembly)
    {
        var all     = DiscoverMigrations(assembly);
        var applied = GetAppliedMigrations().ToHashSet();

        Console.WriteLine("\n=== Status migracija ===");
        Console.WriteLine($"{"Naziv",-50} {"Status",-12} {"Primijenjeno"}");
        Console.WriteLine(new string('-', 80));

        foreach (var m in all)
        {
            var status     = applied.Contains(m.Name) ? "Primijenjeno" : "Pending";
            var appliedAt  = GetAppliedAt(m.Name);
            var appliedStr = appliedAt.HasValue ? appliedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-";
            Console.WriteLine($"{m.Name,-50} {status,-12} {appliedStr}");
        }

        Console.WriteLine();
    }

    // ── Internals ────────────────────────────────────────────────────────────

    private void ApplyUp(Migration migration)
    {
        Console.WriteLine($"Primjenjujem: {migration.Name}...");

        using var transaction = _connection.BeginTransaction();
        try
        {
            migration.Up(_connection);
            RecordApplied(migration.Name, transaction);
            transaction.Commit();
            Console.WriteLine($"  OK: {migration.Name}");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"  GREŠKA pri '{migration.Name}': {ex.Message}");
            throw;
        }
    }

    private void ApplyDown(Migration migration)
    {
        Console.WriteLine($"Poništavam: {migration.Name}...");

        using var transaction = _connection.BeginTransaction();
        try
        {
            migration.Down(_connection);
            RemoveRecord(migration.Name, transaction);
            transaction.Commit();
            Console.WriteLine($"  OK (rollback): {migration.Name}");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"  GREŠKA pri rollback '{migration.Name}': {ex.Message}");
            throw;
        }
    }

    // ── __Migrations tablica ─────────────────────────────────────────────────

    private void EnsureHistoryTable()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS "__Migrations" (
                "Id"            INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                "MigrationName" VARCHAR(255) NOT NULL UNIQUE,
                "AppliedAt"     TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW()
            );
            """;

        using var cmd = new NpgsqlCommand(sql, _connection);
        cmd.ExecuteNonQuery();
    }

    private List<string> GetAppliedMigrations()
    {
        const string sql = """
            SELECT "MigrationName" FROM "__Migrations" ORDER BY "AppliedAt";
            """;

        var result = new List<string>();
        using var cmd    = new NpgsqlCommand(sql, _connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
            result.Add(reader.GetString(0));

        return result;
    }

    private DateTime? GetAppliedAt(string migrationName)
    {
        const string sql = """
            SELECT "AppliedAt" FROM "__Migrations" WHERE "MigrationName" = @name;
            """;

        using var cmd = new NpgsqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@name", migrationName);
        var result = cmd.ExecuteScalar();
        return result is null or DBNull ? null : (DateTime?)Convert.ToDateTime(result);
    }

    private void RecordApplied(string migrationName, NpgsqlTransaction transaction)
    {
        const string sql = """
            INSERT INTO "__Migrations" ("MigrationName", "AppliedAt")
            VALUES (@name, NOW());
            """;

        using var cmd = new NpgsqlCommand(sql, _connection, transaction);
        cmd.Parameters.AddWithValue("@name", migrationName);
        cmd.ExecuteNonQuery();
    }

    private void RemoveRecord(string migrationName, NpgsqlTransaction transaction)
    {
        const string sql = """
            DELETE FROM "__Migrations" WHERE "MigrationName" = @name;
            """;

        using var cmd = new NpgsqlCommand(sql, _connection, transaction);
        cmd.Parameters.AddWithValue("@name", migrationName);
        cmd.ExecuteNonQuery();
    }

    // ── Discovery ────────────────────────────────────────────────────────────

    /// <summary>
    /// Pronalazi sve konkretne podklase Migration u danom assembly-ju,
    /// sortira ih po imenu (YYYYMMDD_HHMMSS konvencija osigurava kronološki red).
    /// </summary>
    private static List<Migration> DiscoverMigrations(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract)
            .OrderBy(t => t.Name)
            .Select(t => (Migration)Activator.CreateInstance(t)!)
            .ToList();
    }
}
