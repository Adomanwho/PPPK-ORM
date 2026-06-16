using MedicalApp.Console.Data;
using MedicalApp.Console.Menus;
using MedicalApp.Entities;
using Npgsql;
using ORM.Core.Migrations;
using System.Reflection;

var connectionString = "Host=localhost;Port=5432;Username=admin;Password=admin;Database=MedicalAppDB";

System.Console.WriteLine("Pokretanje Medicinskog sustava...");

// ── Migracije ─────────────────────────────────────────────────────────────────
// Koristimo zasebnu konekciju za migration runner jer DbContext upravlja svojom.
using var migrationConnection = new NpgsqlConnection(connectionString);
migrationConnection.Open();

var runner = new MigrationRunner(migrationConnection);

// Automatski generiramo i primjenjujemo diff migraciju pri svakom pokretanju
var generator = new MigrationGenerator(migrationConnection);

var entityTypes = new[]
{
    typeof(Lijecnik),
    typeof(Pacijent),
    typeof(PovijestBolesti),
    typeof(Lijek),
    typeof(PrepisanLijek),
    typeof(SpecijalistickiPregled)
};

var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
var diffMigration = generator.GenerateDiff(entityTypes, $"{timestamp}_AutoDiff");

if (diffMigration is not null)
{
    System.Console.WriteLine("\nPronadene promjene sheme:");
    diffMigration.Preview();
    System.Console.Write("\nPrimijeni migraciju? (d/n): ");
    if (System.Console.ReadLine()?.Trim().ToLower() == "d")
        runner.Migrate(diffMigration);
}
else
{
    System.Console.WriteLine("Shema je aktualna — nema pending migracija.");
}

// Ručno pisane migracije iz konzolnog projekta (npr. seed podataka)
runner.Migrate(Assembly.GetExecutingAssembly());

// ── DbContext & seed liječnika ────────────────────────────────────────────────
using var ctx = new MedicalDbContext(connectionString);
LijecnikSeeder.SeedAkoPotrebno(ctx);

// ── Glavni izbornik ───────────────────────────────────────────────────────────
GlavniIzbornik.Pokreni(ctx);
