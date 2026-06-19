using MedicalApp.Console.Data;
using MedicalApp.Console.Menus;
using MedicalApp.Entities;
using Npgsql;
using ORM.Core.Migrations;
using System.Reflection;

var connectionString = "Host=localhost;Port=5432;Username=admin;Password=admin;Database=MedicalAppDB";

System.Console.WriteLine("Pokretanje Medicinskog sustava...");

// ── Migracije ─────────────────────────────────────────────────────────────────
// Zasebna konekcija za MigrationRunner jer DbContext upravlja svojom.
using var migrationConnection = new NpgsqlConnection(connectionString);
migrationConnection.Open();

var runner   = new MigrationRunner(migrationConnection);
var assembly = Assembly.GetExecutingAssembly();

// ── [TOGGLE] Auto-diff migracija ──────────────────────────────────────────────
// Uspoređuje entity klase s trenutnim stanjem baze i nudi primjenu razlika.
// Komentiraj cijeli blok ako ne želiš provjeru sheme pri pokretanju.

var generator   = new MigrationGenerator(migrationConnection);
var entityTypes = new[]
{
    typeof(Lijecnik),
    typeof(Pacijent),
    typeof(PovijestBolesti),
    typeof(Lijek),
    typeof(PrepisanLijek),
    typeof(SpecijalistickiPregled)
};

var timestamp     = DateTime.Now.ToString("yyyyMMdd_HHmmss");
var diffMigration = generator.GenerateDiff(entityTypes, $"{timestamp}_AutoDiff");

if (diffMigration is not null)
{
    runner.RegisterMigration(diffMigration); // dostupno za rollback u izborniku
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

// ── [TOGGLE] Ručno pisane migracije iz ovog projekta ─────────────────────────
// Pronalazi sve klase koje nasljeđuju Migration u ovom assemblyu i izvršava
// one koje još nisu primijenjene (evidencija u __Migrations tablici).
// Komentiraj ako nemaš ručno pisanih migracija ili ih ne želiš primjenjivati.

runner.Migrate(assembly);

// ── DbContext & seed liječnika ────────────────────────────────────────────────
using var ctx = new MedicalDbContext(connectionString);
LijecnikSeeder.SeedAkoPotrebno(ctx);

// ── Glavni izbornik ───────────────────────────────────────────────────────────
GlavniIzbornik.Pokreni(ctx, runner, assembly);
