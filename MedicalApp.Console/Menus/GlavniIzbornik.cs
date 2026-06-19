using MedicalApp.Console.Data;
using ORM.Core.Migrations;
using System.Reflection;

namespace MedicalApp.Console.Menus;

public static class GlavniIzbornik
{
    public static void Pokreni(MedicalDbContext ctx, MigrationRunner runner, Assembly assembly)
    {
        while (true)
        {
            ConsoleHelper.Naslov("MEDICINSKI SUSTAV — GLAVNI IZBORNIK");
            var izbor = ConsoleHelper.CitajIzbornik([
                "Pacijenti",
                "Povijest bolesti",
                "Lijekovi",
                "Prepisani lijekovi",
                "Specijalistički pregledi",
                "Status migracija"
            ]);

            switch (izbor)
            {
                case 0:
                    ConsoleHelper.Info("Zatvaranje aplikacije...");
                    return;
                case 1: PacijentIzbornik.Pokreni(ctx); break;
                case 2: PovijestBolestriIzbornik.Pokreni(ctx); break;
                case 3: LijekIzbornik.Pokreni(ctx); break;
                case 4: PrepisanLijekIzbornik.Pokreni(ctx); break;
                case 5: SpecijalistickiPregledIzbornik.Pokreni(ctx); break;
                case 6: MigrationIzbornik.Pokreni(runner, assembly); break;
            }
        }
    }
}
