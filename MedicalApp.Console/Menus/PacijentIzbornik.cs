using MedicalApp.Console.Data;
using MedicalApp.Entities;
using MedicalApp.Entities.Enums;

namespace MedicalApp.Console.Menus;

public static class PacijentIzbornik
{
    public static void Pokreni(MedicalDbContext ctx)
    {
        while (true)
        {
            ConsoleHelper.Naslov("PACIJENTI");
            var izbor = ConsoleHelper.CitajIzbornik([
                "Prikaz svih pacijenata",
                "Dodaj pacijenta",
                "Uredi pacijenta",
                "Obriši pacijenta"
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
        ConsoleHelper.Naslov("POPIS PACIJENATA");
        var pacijenti = ctx.Pacijenti
            .OrderBy(p => p.Prezime)
            .ToList();

        if (pacijenti.Count == 0)
        {
            ConsoleHelper.Info("Nema pacijenata u sustavu.");
        }
        else
        {
            System.Console.WriteLine($"  {"ID",-5} {"Ime i prezime",-25} {"OIB",-12} {"Datum rođenja",-15} {"Spol",-8}");
            System.Console.WriteLine($"  {new string('-', 70)}");
            foreach (var p in pacijenti)
                System.Console.WriteLine(
                    $"  {p.Id,-5} {p.Ime + " " + p.Prezime,-25} {p.OIB,-12} {p.DatumRodjenja.ToString("dd.MM.yyyy"),-15} {p.Spol,-8}");
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void Dodaj(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("DODAJ PACIJENTA");

        var pacijent = new Pacijent
        {
            Ime               = ConsoleHelper.CitajString("Ime"),
            Prezime           = ConsoleHelper.CitajString("Prezime"),
            OIB               = ConsoleHelper.CitajString("OIB (11 znamenki)"),
            DatumRodjenja     = ConsoleHelper.CitajDatum("Datum rođenja"),
            Spol              = ConsoleHelper.CitajEnum<Spol>("Spol"),
            AdresaBoravista   = ConsoleHelper.CitajString("Adresa boravišta"),
            AdresaPrebivalista= ConsoleHelper.CitajString("Adresa prebivališta")
        };

        try
        {
            ctx.Pacijenti.Add(pacijent);
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh($"Pacijent {pacijent} uspješno dodan (ID: {pacijent.Id}).");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška pri dodavanju: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void Uredi(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("UREDI PACIJENTA");
        var id = ConsoleHelper.CitajInt("ID pacijenta");
        var pacijent = ctx.Pacijenti.Find(id);

        if (pacijent is null)
        {
            ConsoleHelper.Greška($"Pacijent s ID-om {id} nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        ConsoleHelper.Info($"Uređujete: {pacijent} (Enter za zadržavanje trenutne vrijednosti)");
        System.Console.WriteLine();

        var imeNovo = ConsoleHelper.CitajStringOpcional($"Ime [{pacijent.Ime}]");
        if (imeNovo is not null) pacijent.Ime = imeNovo;

        var prezimeNovo = ConsoleHelper.CitajStringOpcional($"Prezime [{pacijent.Prezime}]");
        if (prezimeNovo is not null) pacijent.Prezime = prezimeNovo;

        var oibNovo = ConsoleHelper.CitajStringOpcional($"OIB [{pacijent.OIB}]");
        if (oibNovo is not null) pacijent.OIB = oibNovo;

        var adresaB = ConsoleHelper.CitajStringOpcional($"Adresa boravišta [{pacijent.AdresaBoravista}]");
        if (adresaB is not null) pacijent.AdresaBoravista = adresaB;

        var adresaP = ConsoleHelper.CitajStringOpcional($"Adresa prebivališta [{pacijent.AdresaPrebivalista}]");
        if (adresaP is not null) pacijent.AdresaPrebivalista = adresaP;

        try
        {
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh("Pacijent uspješno ažuriran.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška pri ažuriranju: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void Obrisi(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("OBRIŠI PACIJENTA");
        var id = ConsoleHelper.CitajInt("ID pacijenta");
        var pacijent = ctx.Pacijenti.Find(id);

        if (pacijent is null)
        {
            ConsoleHelper.Greška($"Pacijent s ID-om {id} nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        System.Console.Write($"\n  Brisanje pacijenta '{pacijent}'. Potvrdite (d/n): ");
        if (System.Console.ReadLine()?.Trim().ToLower() != "d")
        {
            ConsoleHelper.Info("Brisanje otkazano.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        try
        {
            ctx.Pacijenti.Remove(pacijent);
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh("Pacijent obrisan.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška pri brisanju: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }
}
