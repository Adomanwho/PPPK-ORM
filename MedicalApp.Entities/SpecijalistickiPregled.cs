using MedicalApp.Entities.Enums;
using ORM.Core.Attributes;

namespace MedicalApp.Entities;

[Table("SpecijalistickiPregledi")]
public class SpecijalistickiPregled
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [Column("PacijentId")]
    public int PacijentId { get; set; }

    /// <summary>Liječnik koji zakazuje pregled.</summary>
    [Required]
    [Column("LijecnikId")]
    public int LijecnikId { get; set; }

    /// <summary>Specijalist koji provodi pregled.</summary>
    [Required]
    [Column("LijecnikSpecijalistId")]
    public int LijecnikSpecijalistId { get; set; }

    [Required]
    [Column("VrstaPregleda")]
    public VrstaPregleda VrstaPregleda { get; set; }

    [Required]
    [Column("Termin")]
    public DateTime Termin { get; set; }

    [Column("Napomena")]
    public string? Napomena { get; set; }

    // Navigacijska svojstva
    [ForeignKey(nameof(PacijentId))]
    public Pacijent Pacijent { get; set; } = null!;

    [ForeignKey(nameof(LijecnikId))]
    public Lijecnik Lijecnik { get; set; } = null!;

    [ForeignKey(nameof(LijecnikSpecijalistId))]
    public Lijecnik LijecnikSpecijalist { get; set; } = null!;

    public override string ToString() =>
        $"{VrstaPregleda} — {Termin:dd.MM.yyyy HH:mm}";
}
