using Npgsql;

namespace ORM.Core.Migrations;

/*
Apstraktna baza za svaku migraciju.
Konkretne migracije nasljeđuju ovu klasu i implementiraju Up i Down.
Konvencija imenovanja: "YYYYMMDD_HHMMSS_Naziv" — runner sortira po imenu klase.
*/
public abstract class Migration
{
    // Ime migracije — defaultno ime klase, može se override-ati.
    public virtual string Name => GetType().Name;

    // Primijeni migraciju (naprijed).
    public abstract void Up(NpgsqlConnection connection);

    // Poništi migraciju (unazad).
    public abstract void Down(NpgsqlConnection connection);

    // Pomoćna metoda za izvršavanje SQL-a unutar Up/Down.
    protected static void Execute(NpgsqlConnection connection, string sql)
    {
        using var cmd = new NpgsqlCommand(sql, connection);
        cmd.ExecuteNonQuery();
    }
}
