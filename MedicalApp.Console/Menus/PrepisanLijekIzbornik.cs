using MedicalApp.Console.Data;
using MedicalApp.Entities;

namespace MedicalApp.Console.Menus;

public static class PrepisanLijekIzbornik
{
    public static void Pokreni(MedicalDbContext ctx)
    {
        while (true)
        {
            ConsoleHelper.Naslov("PREPISANI LIJEKOVI");
            var izbor = ConsoleHelper.CitajIzbornik([
                "Prikaz prepisanih lijekova pacijenta",
                "Prepiši lijek pacijentu",
                "Uredi prepisani lijek",
                "Zaključi terapiju",
                "Obriši prepisani lijek"
            ]);

            switch (izbor)
            {
                case 0: return;
                case 1: PrikaziZaPacijenta(ctx); break;
                case 2: Dodaj(ctx); break;
                case 3: Uredi(ctx); break;
                case 4: ZakljuciTerapiju(ctx); break;
                case 5: Obrisi(ctx); break;
            }
        }
    }

    private static void PrikaziZaPacijenta(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("PREPISANI LIJEKOVI PACIJENTA");
        var pacijentId = ConsoleHelper.CitajInt("ID pacijenta");

        var pacijent = ctx.Pacijenti.Find(pacijentId);
        if (pacijent is null)
        {
            ConsoleHelper.Greška("Pacijent nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        var stavke = ctx.PrepisaniLijekovi
            .Where(pl => pl.PacijentId == pacijentId)
            .Include(pl => pl.Lijek)
            .Include(pl => pl.Lijecnik)
            .OrderByDescending(pl => pl.DatumPrepisivanja)
            .ToList();

        ConsoleHelper.Info($"Pacijent: {pacijent}");
        System.Console.WriteLine();

        if (stavke.Count == 0)
        {
            ConsoleHelper.Info("Nema prepisanih lijekova.");
        }
        else
        {
            System.Console.WriteLine($"  {"ID",-5} {"Lijek",-20} {"Doza",-12} {"Učestalost",-20} {"Od",-12} {"Do",-12}");
            System.Console.WriteLine($"  {new string('-', 85)}");
            foreach (var s in stavke)
            {
                var naziv = s.Lijek?.Naziv ?? $"ID {s.LijekId}";
                var do_   = s.DatumZavrsetka.HasValue ? s.DatumZavrsetka.Value.ToString("dd.MM.yyyy") : "aktivno";
                System.Console.WriteLine(
                    $"  {s.Id,-5} {naziv,-20} {s.Doza,-12} {s.Ucestalost,-20} {s.DatumPrepisivanja.ToString("dd.MM.yyyy"),-12} {do_,-12}");
                if (!string.IsNullOrWhiteSpace(s.Napomena))
                    System.Console.WriteLine($"        Napomena: {s.Napomena}");
            }
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void Dodaj(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("PREPIŠI LIJEK PACIJENTU");

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

        int lijecnikId;
        while (true)
        {
            lijecnikId = ConsoleHelper.CitajInt("ID liječnika");
            if (lijecnici.Any(l => l.Id == lijecnikId)) break;
            ConsoleHelper.Greška("Odabrani liječnik ne postoji. Pokušajte ponovo.");
        }

        var lijekovi = ctx.Lijekovi.OrderBy(l => l.Naziv).ToList();
        ConsoleHelper.Info("Dostupni lijekovi:");
        foreach (var l in lijekovi)
            System.Console.WriteLine($"    {l.Id}. {l}");

        int lijekId;
        while (true)
        {
            lijekId = ConsoleHelper.CitajInt("ID lijeka");
            if (lijekovi.Any(l => l.Id == lijekId)) break;
            ConsoleHelper.Greška("Odabrani lijek ne postoji. Pokušajte ponovo.");
        }

        var datumPrepisivanja = ConsoleHelper.CitajDatum("Datum prepisivanja");
        DateTime? datumZavrsetka;
        while (true)
        {
            datumZavrsetka = ConsoleHelper.CitajDatumOpcional("Datum završetka terapije");
            if (!datumZavrsetka.HasValue || datumZavrsetka.Value > datumPrepisivanja) break;
            ConsoleHelper.Greška("Datum završetka mora biti nakon datuma prepisivanja. Pokušajte ponovo.");
        }

        var unos = new PrepisanLijek
        {
            PacijentId        = pacijentId,
            LijecnikId        = lijecnikId,
            LijekId           = lijekId,
            Doza              = ConsoleHelper.CitajString("Doza (npr. 500mg, 2 tablete)"),
            Ucestalost        = ConsoleHelper.CitajString("Učestalost (npr. 3 puta dnevno)"),
            DatumPrepisivanja = datumPrepisivanja,
            DatumZavrsetka    = datumZavrsetka,
            Napomena          = ConsoleHelper.CitajStringOpcional("Napomena")
        };

        try
        {
            ctx.PrepisaniLijekovi.Add(unos);
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh($"Lijek prepisan (ID: {unos.Id}).");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void Uredi(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("UREDI PREPISANI LIJEK");
        var id = ConsoleHelper.CitajInt("ID prepisanog lijeka");
        var unos = ctx.PrepisaniLijekovi.Find(id);

        if (unos is null)
        {
            ConsoleHelper.Greška("Unos nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        var doza = ConsoleHelper.CitajStringOpcional($"Doza [{unos.Doza}]");
        if (doza is not null) unos.Doza = doza;

        var ucestalost = ConsoleHelper.CitajStringOpcional($"Učestalost [{unos.Ucestalost}]");
        if (ucestalost is not null) unos.Ucestalost = ucestalost;

        var napomena = ConsoleHelper.CitajStringOpcional($"Napomena [{unos.Napomena}]");
        if (napomena is not null) unos.Napomena = napomena;

        try
        {
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh("Prepisani lijek ažuriran.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void ZakljuciTerapiju(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("ZAKLJUČI TERAPIJU");
        var id = ConsoleHelper.CitajInt("ID prepisanog lijeka");
        var unos = ctx.PrepisaniLijekovi.Find(id);

        if (unos is null)
        {
            ConsoleHelper.Greška("Unos nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        if (unos.DatumZavrsetka.HasValue)
        {
            ConsoleHelper.Info($"Datum već unesen: {unos.DatumZavrsetka.Value:dd.MM.yyyy}.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        DateTime novDatum;
        while (true)
        {
            novDatum = ConsoleHelper.CitajDatum("Datum završetka terapije");
            if (novDatum > unos.DatumPrepisivanja) break;
            ConsoleHelper.Greška("Datum završetka mora biti nakon datuma prepisivanja. Pokušajte ponovo.");
        }

        unos.DatumZavrsetka = novDatum;

        try
        {
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh("Terapija zaključena.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void Obrisi(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("OBRIŠI PREPISANI LIJEK");
        var id = ConsoleHelper.CitajInt("ID prepisanog lijeka");
        var unos = ctx.PrepisaniLijekovi.Find(id);

        if (unos is null)
        {
            ConsoleHelper.Greška("Unos nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        System.Console.Write($"\n  Brisanje prepisanog lijeka ID {id}. Potvrdite (d/n): ");
        if (System.Console.ReadLine()?.Trim().ToLower() != "d")
        {
            ConsoleHelper.Info("Brisanje otkazano.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        try
        {
            ctx.PrepisaniLijekovi.Remove(unos);
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh("Prepisani lijek obrisan.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }
}
