using MedicalApp.Console.Data;
using MedicalApp.Entities;

namespace MedicalApp.Console.Menus;

public static class LijecnikSeeder
{
    /*
     Ako u bazi nema niti jednog liječnika, traži unos pri pokretanju aplikacije.
     Poziva se samo jednom — pri prvom pokretanju.
    */
    public static void SeedAkoPotrebno(MedicalDbContext ctx)
    {
        var postoje = ctx.Lijecnici.ToList();
        if (postoje.Count > 0) return;

        ConsoleHelper.Naslov("PRVO POKRETANJE — Unos liječnika");
        ConsoleHelper.Info("U bazi nema liječnika. Unesite barem jednog liječnika.");
        ConsoleHelper.Info("Liječnici se definiraju samo pri prvom pokretanju.");
        System.Console.WriteLine();

        while (true)
        {
            var lijecnik = new Lijecnik
            {
                Ime             = ConsoleHelper.CitajString("Ime"),
                Prezime         = ConsoleHelper.CitajString("Prezime"),
                Specijalizacija = ConsoleHelper.CitajString("Specijalizacija")
            };

            ctx.Lijecnici.Add(lijecnik);
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh($"Liječnik {lijecnik} dodan.");

            System.Console.Write("\n  Dodati još jednog liječnika? (d/n): ");
            var odgovor = System.Console.ReadLine()?.Trim().ToLower();
            if (odgovor != "d") break;
        }
    }
}
