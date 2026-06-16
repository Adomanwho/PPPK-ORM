using MedicalApp.Console.Data;
using MedicalApp.Entities;
using MedicalApp.Entities.Enums;

namespace MedicalApp.Console.Menus;

public static class SpecijalistickiPregledIzbornik
{
    public static void Pokreni(MedicalDbContext ctx)
    {
        while (true)
        {
            ConsoleHelper.Naslov("SPECIJALISTIČKI PREGLEDI");
            var izbor = ConsoleHelper.CitajIzbornik([
                "Prikaz pregleda pacijenta",
                "Zakaži pregled",
                "Uredi pregled",
                "Obriši pregled"
            ]);

            switch (izbor)
            {
                case 0: return;
                case 1: PrikaziZaPacijenta(ctx); break;
                case 2: Dodaj(ctx); break;
                case 3: Uredi(ctx); break;
                case 4: Obrisi(ctx); break;
            }
        }
    }

    private static void PrikaziZaPacijenta(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("SPECIJALISTIČKI PREGLEDI PACIJENTA");
        var pacijentId = ConsoleHelper.CitajInt("ID pacijenta");

        var pacijent = ctx.Pacijenti.Find(pacijentId);
        if (pacijent is null)
        {
            ConsoleHelper.Greška("Pacijent nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        var pregledi = ctx.SpecijalistickiPregledi
            .Where(sp => sp.PacijentId == pacijentId)
            .Include(sp => sp.LijecnikSpecijalist)
            .OrderBy(sp => sp.Termin)
            .ToList();

        ConsoleHelper.Info($"Pacijent: {pacijent}");
        System.Console.WriteLine();

        if (pregledi.Count == 0)
        {
            ConsoleHelper.Info("Nema zakazanih pregleda.");
        }
        else
        {
            System.Console.WriteLine($"  {"ID",-5} {"Vrsta",-8} {"Termin",-20} {"Specijalist",-25} {"Napomena",-25}");
            System.Console.WriteLine($"  {new string('-', 88)}");
            foreach (var p in pregledi)
            {
                var spec = p.LijecnikSpecijalist is not null ? p.LijecnikSpecijalist.ToString() : $"ID {p.LijecnikSpecijalistId}";
                System.Console.WriteLine(
                    $"  {p.Id,-5} {p.VrstaPregleda,-8} {p.Termin.ToString("dd.MM.yyyy HH:mm"),-20} {spec,-25} {p.Napomena ?? "-",-25}");
            }
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void Dodaj(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("ZAKAŽI SPECIJALISTIČKI PREGLED");

        var pacijentId = ConsoleHelper.CitajInt("ID pacijenta");
        if (ctx.Pacijenti.Find(pacijentId) is null)
        {
            ConsoleHelper.Greška("Pacijent nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        var lijecnici = ctx.Lijecnici.OrderBy(l => l.Prezime).ToList();
        ConsoleHelper.Info("Dostupni liječnici:");
        foreach (var l in lijecnici)
            System.Console.WriteLine($"    {l.Id}. {l}");

        var lijecnikId = ConsoleHelper.CitajInt("ID liječnika koji zakazuje");
        var specijalistId = ConsoleHelper.CitajInt("ID liječnika specijalista koji provodi pregled");

        var pregled = new SpecijalistickiPregled
        {
            PacijentId            = pacijentId,
            LijecnikId            = lijecnikId,
            LijecnikSpecijalistId = specijalistId,
            VrstaPregleda         = ConsoleHelper.CitajEnum<VrstaPregleda>("Vrsta pregleda"),
            Termin                = ConsoleHelper.CitajDatumVrijeme("Termin pregleda"),
            Napomena              = ConsoleHelper.CitajStringOpcional("Napomena")
        };

        try
        {
            ctx.SpecijalistickiPregledi.Add(pregled);
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh($"Pregled zakazan (ID: {pregled.Id}).");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void Uredi(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("UREDI PREGLED");
        var id = ConsoleHelper.CitajInt("ID pregleda");
        var pregled = ctx.SpecijalistickiPregledi.Find(id);

        if (pregled is null)
        {
            ConsoleHelper.Greška("Pregled nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        ConsoleHelper.Info($"Uređujete: {pregled}");

        System.Console.Write($"  Promijeniti termin [{pregled.Termin:dd.MM.yyyy HH:mm}]? (d/n): ");
        if (System.Console.ReadLine()?.Trim().ToLower() == "d")
            pregled.Termin = ConsoleHelper.CitajDatumVrijeme("Novi termin");

        var napomena = ConsoleHelper.CitajStringOpcional($"Napomena [{pregled.Napomena}]");
        if (napomena is not null) pregled.Napomena = napomena;

        try
        {
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh("Pregled ažuriran.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void Obrisi(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("OBRIŠI PREGLED");
        var id = ConsoleHelper.CitajInt("ID pregleda");
        var pregled = ctx.SpecijalistickiPregledi.Find(id);

        if (pregled is null)
        {
            ConsoleHelper.Greška("Pregled nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        System.Console.Write($"\n  Brisanje pregleda '{pregled}'. Potvrdite (d/n): ");
        if (System.Console.ReadLine()?.Trim().ToLower() != "d")
        {
            ConsoleHelper.Info("Brisanje otkazano.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        try
        {
            ctx.SpecijalistickiPregledi.Remove(pregled);
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh("Pregled obrisan.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }
}
