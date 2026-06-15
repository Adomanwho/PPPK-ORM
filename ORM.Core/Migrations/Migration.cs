using Npgsql;

namespace ORM.Core.Migrations;

/// <summary>
/// Apstraktna baza za svaku migraciju.
/// Konkretne migracije nasljeđuju ovu klasu i implementiraju Up i Down.
/// Konvencija imenovanja: "YYYYMMDD_HHMMSS_Naziv" — runner sortira po imenu klase.
/// </summary>
public abstract class Migration
{
    /// <summary>Ime migracije — defaultno ime klase, može se override-ati.</summary>
    public virtual string Name => GetType().Name;

    /// <summary>Primijeni migraciju (naprijed).</summary>
    public abstract void Up(NpgsqlConnection connection);

    /// <summary>Poništi migraciju (unazad).</summary>
    public abstract void Down(NpgsqlConnection connection);

    /// <summary>Pomoćna metoda za izvršavanje SQL-a unutar Up/Down.</summary>
    protected static void Execute(NpgsqlConnection connection, string sql)
    {
        using var cmd = new NpgsqlCommand(sql, connection);
        cmd.ExecuteNonQuery();
    }
}
