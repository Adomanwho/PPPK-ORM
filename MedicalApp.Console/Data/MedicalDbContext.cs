using MedicalApp.Entities;
using ORM.Core.Context;

namespace MedicalApp.Console.Data;

public class MedicalDbContext : DbContext
{
    public DbSet<Lijecnik> Lijecnici { get; set; } = null!;
    public DbSet<Pacijent> Pacijenti { get; set; } = null!;
    public DbSet<PovijestBolesti> PovijestiBolesti { get; set; } = null!;
    public DbSet<Lijek> Lijekovi { get; set; } = null!;
    public DbSet<PrepisanLijek> PrepisaniLijekovi { get; set; } = null!;
    public DbSet<SpecijalistickiPregled> SpecijalistickiPregledi { get; set; } = null!;

    public MedicalDbContext(string connectionString) : base(connectionString) { }
}
