using ORM.Core.Attributes;

namespace MedicalApp.Entities;

[Table("PrepisaniLijekovi")]
public class PrepisanLijek
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [Column("PacijentId")]
    public int PacijentId { get; set; }

    [Required]
    [Column("LijecnikId")]
    public int LijecnikId { get; set; }

    [Required]
    [Column("LijekId")]
    public int LijekId { get; set; }

    /// <summary>Npr. "500mg", "2 tablete", "10 jedinica".</summary>
    [Required]
    [MaxLength(100)]
    [Column("Doza")]
    public string Doza { get; set; } = null!;

    /// <summary>Npr. "3 puta dnevno", "svaki drugi dan", "jednom u dva tjedna".</summary>
    [Required]
    [MaxLength(150)]
    [Column("Ucestalost")]
    public string Ucestalost { get; set; } = null!;

    [Required]
    [Column("DatumPrepisivanja")]
    public DateTime DatumPrepisivanja { get; set; }

    /// <summary>Null znači terapija još traje.</summary>
    [Column("DatumZavrsetka")]
    public DateTime? DatumZavrsetka { get; set; }

    [Column("Napomena")]
    public string? Napomena { get; set; }

    // Navigacijska svojstva
    [ForeignKey(nameof(PacijentId))]
    public Pacijent Pacijent { get; set; } = null!;

    [ForeignKey(nameof(LijecnikId))]
    public Lijecnik Lijecnik { get; set; } = null!;

    [ForeignKey(nameof(LijekId))]
    public Lijek Lijek { get; set; } = null!;

    public override string ToString() =>
        $"{Lijek?.Naziv ?? $"LijekId={LijekId}"} — {Doza}, {Ucestalost}";
}
