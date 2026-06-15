using MedicalApp.Entities.Enums;
using ORM.Core.Attributes;

namespace MedicalApp.Entities;

[Table("Lijecnici")]
public class Lijecnik
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
    [MaxLength(100)]
    [Column("Specijalizacija")]
    public string Specijalizacija { get; set; } = null!;

    // Navigacijska svojstva
    [InverseProperty(nameof(PovijestBolesti.Lijecnik))]
    public ICollection<PovijestBolesti> PovijestiBolesti { get; set; } = [];

    [InverseProperty(nameof(PrepisanLijek.Lijecnik))]
    public ICollection<PrepisanLijek> PrepisaniLijekovi { get; set; } = [];

    [InverseProperty(nameof(SpecijalistickiPregled.LijecnikSpecijalist))]
    public ICollection<SpecijalistickiPregled> SpecijalistickiPregledi { get; set; } = [];

    public override string ToString() =>
        $"Dr. {Ime} {Prezime} ({Specijalizacija})";
}
