using MedicalApp.Console.Data;
using MedicalApp.Entities;

namespace MedicalApp.Console.Menus;

public static class PovijestBolestriIzbornik
{
    public static void Pokreni(MedicalDbContext ctx)
    {
        while (true)
        {
            ConsoleHelper.Naslov("POVIJEST BOLESTI");
            var izbor = ConsoleHelper.CitajIzbornik([
                "Prikaz povijesti bolesti pacijenta",
                "Dodaj unos u povijest bolesti",
                "Uredi unos",
                "Zatvori dijagnozu (postavi datum završetka)",
                "Obriši unos"
            ]);

            switch (izbor)
            {
                case 0: return;
                case 1: PrikaziZaPacijenta(ctx); break;
                case 2: Dodaj(ctx); break;
                case 3: Uredi(ctx); break;
                case 4: ZatvoriDijagnozu(ctx); break;
                case 5: Obrisi(ctx); break;
            }
        }
    }

    private static void PrikaziZaPacijenta(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("POVIJEST BOLESTI PACIJENTA");
        var pacijentId = ConsoleHelper.CitajInt("ID pacijenta");

        var pacijent = ctx.Pacijenti.Find(pacijentId);
        if (pacijent is null)
        {
            ConsoleHelper.Greška("Pacijent nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        var stavke = ctx.PovijestiBolesti
            .Where(p => p.PacijentId == pacijentId)
            .Include(p => p.Lijecnik)
            .OrderByDescending(p => p.DatumOd)
            .ToList();

        ConsoleHelper.Info($"Pacijent: {pacijent}");
        System.Console.WriteLine();

        if (stavke.Count == 0)
        {
            ConsoleHelper.Info("Nema unesenih stavki u povijest bolesti.");
        }
        else
        {
            System.Console.WriteLine($"  {"ID",-5} {"Dijagnoza",-30} {"Od",-12} {"Do",-12} {"Liječnik",-25}");
            System.Console.WriteLine($"  {new string('-', 90)}");
            foreach (var s in stavke)
            {
                var datumDo = s.DatumDo.HasValue ? s.DatumDo.Value.ToString("dd.MM.yyyy") : "aktivno";
                var lijecnik = s.Lijecnik is not null ? s.Lijecnik.ToString() : $"ID {s.LijecnikId}";
                System.Console.WriteLine(
                    $"  {s.Id,-5} {s.Dijagnoza,-30} {s.DatumOd.ToString("dd.MM.yyyy"),-12} {datumDo,-12} {lijecnik,-25}");
                if (!string.IsNullOrWhiteSpace(s.Napomena))
                    System.Console.WriteLine($"        Napomena: {s.Napomena}");
            }
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void Dodaj(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("DODAJ POVIJEST BOLESTI");

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

        var datumOd = ConsoleHelper.CitajDatum("Datum od");
        DateTime? datumDo;
        while (true)
        {
            datumDo = ConsoleHelper.CitajDatumOpcional("Datum do (ostavite prazno ako traje)");
            if (!datumDo.HasValue || datumDo.Value > datumOd) break;
            ConsoleHelper.Greška("Datum do mora biti nakon datuma od. Pokušajte ponovo.");
        }

        var unos = new PovijestBolesti
        {
            PacijentId = pacijentId,
            LijecnikId = lijecnikId,
            Dijagnoza  = ConsoleHelper.CitajString("Dijagnoza"),
            DatumOd   = datumOd,
            DatumDo   = datumDo,
            Napomena  = ConsoleHelper.CitajStringOpcional("Napomena")
        };

        try
        {
            ctx.PovijestiBolesti.Add(unos);
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh($"Unos dodan (ID: {unos.Id}).");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void Uredi(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("UREDI POVIJEST BOLESTI");
        var id = ConsoleHelper.CitajInt("ID unosa");
        var unos = ctx.PovijestiBolesti.Find(id);

        if (unos is null)
        {
            ConsoleHelper.Greška("Unos nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        ConsoleHelper.Info($"Uređujete: {unos}");

        var dijagnoza = ConsoleHelper.CitajStringOpcional($"Dijagnoza [{unos.Dijagnoza}]");
        if (dijagnoza is not null) unos.Dijagnoza = dijagnoza;

        var napomena = ConsoleHelper.CitajStringOpcional($"Napomena [{unos.Napomena}]");
        if (napomena is not null) unos.Napomena = napomena;

        try
        {
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh("Unos ažuriran.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void ZatvoriDijagnozu(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("ZATVORI DIJAGNOZU");
        var id = ConsoleHelper.CitajInt("ID unosa");
        var unos = ctx.PovijestiBolesti.Find(id);

        if (unos is null)
        {
            ConsoleHelper.Greška("Unos nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        if (unos.DatumDo.HasValue)
        {
            ConsoleHelper.Info($"Datum već unesen: {unos.DatumDo.Value:dd.MM.yyyy}.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        DateTime novDatum;
        while (true)
        {
            novDatum = ConsoleHelper.CitajDatum("Datum završetka");
            if (novDatum > unos.DatumOd) break;
            ConsoleHelper.Greška("Datum završetka mora biti nakon datuma od. Pokušajte ponovo.");
        }

        unos.DatumDo = novDatum;

        try
        {
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh($"Dijagnoza zatvorena: {unos.DatumDo:dd.MM.yyyy}.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }

    private static void Obrisi(MedicalDbContext ctx)
    {
        ConsoleHelper.Naslov("OBRIŠI UNOS POVIJESTI BOLESTI");
        var id = ConsoleHelper.CitajInt("ID unosa");
        var unos = ctx.PovijestiBolesti.Find(id);

        if (unos is null)
        {
            ConsoleHelper.Greška("Unos nije pronađen.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        System.Console.Write($"\n  Brisanje unosa '{unos}'. Potvrdite (d/n): ");
        if (System.Console.ReadLine()?.Trim().ToLower() != "d")
        {
            ConsoleHelper.Info("Brisanje otkazano.");
            ConsoleHelper.PritisniEnter();
            return;
        }

        try
        {
            ctx.PovijestiBolesti.Remove(unos);
            ctx.SaveChanges();
            ConsoleHelper.Uspjeh("Unos obrisan.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Greška($"Greška: {ex.Message}");
        }

        ConsoleHelper.PritisniEnter();
    }
}
