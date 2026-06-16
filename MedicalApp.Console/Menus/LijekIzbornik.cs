using MedicalApp.Console.Data;
using MedicalApp.Entities;

namespace MedicalApp.Console.Menus;

public static class LijekIzbornik
{
    public static void Pokreni(MedicalDbContext ctx)
    {
        while (true)
        {
            ConsoleHelper.Naslov("LIJEKOVI");
            var izbor = ConsoleHelper.CitajIzbornik([
                "Prikaz svih lijekova",
                "Dodaj lijek",
                "Uredi lijek",
                "Obriši lijek"
            ]);

            switch (izbor)
            {
                case 0: return;
                case 1: PrikaziSve(ctx); break;
                case 2: Dodaj(ctx); break;
                case 3: Uredi(ctx); break;
                case 4: Obrisi(ctx); break;
            }
        }
    }

    private static void PrikaziSve(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("POPIS LIJEKOVA");
        var lijekovi = ctx.Lijekovi.OrderBy(l => l.Naziv).ToList();

        if (lijekovi.Count == 0)
        {
            ConsoleHelper.Info("Nema lijekova u katalogu.");
        }
        else
        {
            System.Console.WriteLine($"  {"ID",-5} {"Naziv",-30} {"Aktivna tvar",-25} {"Opis",-30}");
            System.Console.WriteLine($"  {new string('-', 95)}");
            foreach (var l in lijekovi)
                System.Console.WriteLine(
                    $"  {l.Id,-5} {l.Naziv,-30} {l.AktivnaTvar ?? "-",-25} {l.Opis?.Substring(0, Math.Min(l.Opis.Length, 28)) ?? "-",-30}");
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void Dodaj(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("DODAJ LIJEK");

        var lijek = new Lijek
        {
            Naziv        = ConsoleHelper.CitajString("Naziv lijeka"),
            AktivnaTvar  = ConsoleHelper.CitajStringOpcional("Aktivna tvar"),
            Opis         = ConsoleHelper.CitajStringOpcional("Opis")
        };

        try
        {
            ctx.Lijekovi.Add(lijek);
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh($"Lijek '{lijek}' dodan (ID: {lijek.Id}).");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void Uredi(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("UREDI LIJEK");
        var id = ConsoleHelper.CitajInt("ID lijeka");
        var lijek = ctx.Lijekovi.Find(id);

        if (lijek is null)
        {
            ConsoleHelper.Greška("Lijek nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        ConsoleHelper.Info($"Uređujete: {lijek}");

        var naziv = ConsoleHelper.CitajStringOpcional($"Naziv [{lijek.Naziv}]");
        if (naziv is not null) lijek.Naziv = naziv;

        var tvar = ConsoleHelper.CitajStringOpcional($"Aktivna tvar [{lijek.AktivnaTvar}]");
        if (tvar is not null) lijek.AktivnaTvar = tvar;

        var opis = ConsoleHelper.CitajStringOpcional($"Opis [{lijek.Opis}]");
        if (opis is not null) lijek.Opis = opis;

        try
        {
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh("Lijek ažuriran.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void Obrisi(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("OBRIŠI LIJEK");
        var id = ConsoleHelper.CitajInt("ID lijeka");
        var lijek = ctx.Lijekovi.Find(id);

        if (lijek is null)
        {
            ConsoleHelper.Greška("Lijek nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        System.Console.Write($"\n  Brisanje lijeka '{lijek}'. Potvrdite (d/n): ");
        if (System.Console.ReadLine()?.Trim().ToLower() != "d")
        {
            ConsoleHelper.Info("Brisanje otkazano.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        try
        {
            ctx.Lijekovi.Remove(lijek);
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh("Lijek obrisan.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }
}
