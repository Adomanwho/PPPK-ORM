namespace MedicalApp.Console;

using Npgsql;
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Pokrecem ORM projekt!");

        var connectionString = "Host=localhost;Port=5432;Username=admin;Password=admin;Database=MedicalAppDB";

        try
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                Console.WriteLine("Uspjesna veza s PostgreSQL bazom!");

                using (var cmd = new NpgsqlCommand("SELECT version();", conn))
                {
                    var version = cmd.ExecuteScalar();
                    Console.WriteLine($"PostgreSQL verzija: {version}");
                }

                conn.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greska pri povezivanju: {ex.Message}");
            Console.WriteLine("Provjerite je li Docker kontejner pokrenut (docker ps)");
        }

        Console.ReadKey();
    }
}