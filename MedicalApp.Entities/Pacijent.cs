using MedicalApp.Entities.Enums;
using ORM.Core.Attributes;

namespace MedicalApp.Entities;

[Table("Pacijenti")]
public class Pacijent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("Ime")]
    public string Ime { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    [Column("Prezime")]
    public string Prezime { get; set; } = null!;

    [Required]
    [Unique]
    [MaxLength(11)]
    [Column("OIB")]
    public string OIB { get; set; } = null!;

    [Required]
    [Column("DatumRodjenja")]
    public DateTime DatumRodjenja { get; set; }

    [Required]
    [Column("Spol")]
    public Spol Spol { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("AdresaBoravista")]
    public string AdresaBoravista { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    [Column("AdresaPrebivalista")]
    public string AdresaPrebivalista { get; set; } = null!;

    // Navigacijska svojstva
    [InverseProperty(nameof(PovijestBolesti.Pacijent))]
    public ICollection<PovijestBolesti> PovijestiBolesti { get; set; } = [];

    [InverseProperty(nameof(PrepisanLijek.Pacijent))]
    public ICollection<PrepisanLijek> PrepisaniLijekovi { get; set; } = [];

    [InverseProperty(nameof(SpecijalistickiPregled.Pacijent))]
    public ICollection<SpecijalistickiPregled> SpecijalistickiPregledi { get; set; } = [];

    public override string ToString() =>
        $"{Ime} {Prezime} (OIB: {OIB})";
}
