using ORM.Core.Migrations;
using System.Reflection;

namespace MedicalApp.Console.Menus;

public static class MigrationIzbornik
{
    public static void Pokreni(MigrationRunner runner, Assembly assembly)
    {
        while (true)
        {
            ConsoleHelper.Naslov("MIGRACIJE");
            var izbor = ConsoleHelper.CitajIzbornik([
                "Prikaži status migracija",
                "Primijeni pending migracije",
                "Rollback zadnje migracije"
            ]);

            switch (izbor)
            {
                case 0: return;
                case 1:
                    runner.PrintStatus(assembly);
                    ConsoleHelper.PritisniEnter();
                    break;
                case 2:
                    System.Console.Write("\n  Primijeni sve pending migracije? (d/n): ");
                    if (System.Console.ReadLine()?.Trim().ToLower() == "d")
                        runner.Migrate(assembly);
                    ConsoleHelper.PritisniEnter();
                    break;
                case 3:
                    System.Console.Write("\n  Rollback zadnje migracije? (d/n): ");
                    if (System.Console.ReadLine()?.Trim().ToLower() == "d")
                        runner.Rollback(assembly);
                    ConsoleHelper.PritisniEnter();
                    break;
            }
        }
    }
}
