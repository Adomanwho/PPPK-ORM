using ORM.Core.Attributes;

namespace MedicalApp.Entities;

[Table("PovijestiBolesti")]
public class PovijestBolesti
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
    [MaxLength(255)]
    [Column("Dijagnoza")]
    public string Dijagnoza { get; set; } = null!;

    [Required]
    [Column("DatumOd")]
    public DateTime DatumOd { get; set; }

    //Null znači da stanje još traje.
    [Column("DatumDo")]
    public DateTime? DatumDo { get; set; }

    [Column("Napomena")]
    public string? Napomena { get; set; }

    // Navigacijska svojstva
    [ForeignKey(nameof(PacijentId))]
    public Pacijent Pacijent { get; set; } = null!;

    [ForeignKey(nameof(LijecnikId))]
    public Lijecnik Lijecnik { get; set; } = null!;

    public override string ToString()
    {
        var period = DatumDo.HasValue
            ? $"{DatumOd:dd.MM.yyyy} – {DatumDo:dd.MM.yyyy}"
            : $"od {DatumOd:dd.MM.yyyy} (aktivno)";
        return $"{Dijagnoza} [{period}]";
    }
}
