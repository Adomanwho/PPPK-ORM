namespace MedicalApp.Console.Menus;

public static class ConsoleHelper
{
    public static void Naslov(string tekst)
    {
        System.Console.WriteLine();
        System.Console.WriteLine(new string('=', 50));
        System.Console.WriteLine($"  {tekst}");
        System.Console.WriteLine(new string('=', 50));
    }

    public static void Uspjeh(string poruka)
    {
        var boja = System.Console.ForegroundColor;
        System.Console.ForegroundColor = ConsoleColor.Green;
        System.Console.WriteLine($"  [OK] {poruka}");
        System.Console.ForegroundColor = boja;
    }

    public static void Greška(string poruka)
    {
        var boja = System.Console.ForegroundColor;
        System.Console.ForegroundColor = ConsoleColor.Red;
        System.Console.WriteLine($"  [!] {poruka}");
        System.Console.ForegroundColor = boja;
    }

    public static void Info(string poruka) =>
        System.Console.WriteLine($"  {poruka}");

    public static string CitajString(string prompt, bool obavezan = true)
    {
        while (true)
        {
            System.Console.Write($"  {prompt}: ");
            var unos = System.Console.ReadLine()?.Trim() ?? "";
            if (!obavezan || unos.Length > 0)
                return unos;
            Greška("Polje je obavezno.");
        }
    }

    public static string? CitajStringOpcional(string prompt)
    {
        System.Console.Write($"  {prompt} (Enter za preskočiti): ");
        var unos = System.Console.ReadLine()?.Trim();
        return string.IsNullOrWhiteSpace(unos) ? null : unos;
    }

    public static int CitajInt(string prompt)
    {
        while (true)
        {
            System.Console.Write($"  {prompt}: ");
            if (int.TryParse(System.Console.ReadLine(), out var val))
                return val;
            Greška("Unesite cijeli broj.");
        }
    }

    public static DateTime CitajDatum(string prompt)
    {
        while (true)
        {
            System.Console.Write($"  {prompt} (dd.MM.yyyy): ");
            var unos = System.Console.ReadLine()?.Trim();
            if (DateTime.TryParseExact(
                    unos,
                    "dd.MM.yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var dt))
                return dt;
            Greška("Neispravan format datuma. Primjer: 25.03.1990");
        }
    }

    public static DateTime CitajDatumVrijeme(string prompt)
    {
        while (true)
        {
            System.Console.Write($"  {prompt} (dd.MM.yyyy HH:mm): ");
            var unos = System.Console.ReadLine()?.Trim();
            if (DateTime.TryParseExact(
                    unos,
                    "dd.MM.yyyy HH:mm",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var dt))
                return dt;
            Greška("Neispravan format. Primjer: 25.03.2026 14:30");
        }
    }

    public static DateTime? CitajDatumOpcional(string prompt)
    {
        System.Console.Write($"  {prompt} (dd.MM.yyyy, Enter za preskočiti): ");
        var unos = System.Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(unos)) return null;
        if (DateTime.TryParseExact(
                unos, "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var dt))
            return dt;
        Greška("Neispravan format, datum nije postavljen.");
        return null;
    }

    public static T CitajEnum<T>(string prompt) where T : struct, Enum
    {
        var vrijednosti = Enum.GetValues<T>();
        System.Console.WriteLine($"  {prompt}:");
        for (int i = 0; i < vrijednosti.Length; i++)
            System.Console.WriteLine($"    {i + 1}. {vrijednosti[i]}");

        while (true)
        {
            System.Console.Write("  Odabir (broj): ");
            if (int.TryParse(System.Console.ReadLine(), out var idx)
                && idx >= 1 && idx <= vrijednosti.Length)
                return vrijednosti[idx - 1];
            Greška("Nevažeći odabir.");
        }
    }

    public static int CitajIzbornik(string[] opcije)
    {
        for (int i = 0; i < opcije.Length; i++)
            System.Console.WriteLine($"  {i + 1}. {opcije[i]}");
        System.Console.WriteLine($"  0. Natrag");
        System.Console.WriteLine();

        while (true)
        {
            System.Console.Write("  Odabir: ");
            if (int.TryParse(System.Console.ReadLine(), out var izbor)
                && izbor >= 0 && izbor <= opcije.Length)
                return izbor;
            Greška("Nevažeći odabir.");
        }
    }

    public static void PritisniEnter()
    {
        System.Console.WriteLine();
        System.Console.Write("  Pritisnite Enter za nastavak...");
        System.Console.ReadLine();
    }
}
