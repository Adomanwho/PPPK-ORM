using ORM.Core.Attributes;

namespace MedicalApp.Entities;

[Table("Lijekovi")]
public class Lijek
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("Naziv")]
    public string Naziv { get; set; } = null!;

    /// <summary>Aktivna supstanca / generički naziv.</summary>
    [MaxLength(200)]
    [Column("AktivnaTvar")]
    public string? AktivnaTvar { get; set; }

    [Column("Opis")]
    public string? Opis { get; set; }

    // Navigacijsko svojstvo
    [InverseProperty(nameof(PrepisanLijek.Lijek))]
    public ICollection<PrepisanLijek> PrepisaniLijekovi { get; set; } = [];

    public override string ToString() =>
        AktivnaTvar is not null ? $"{Naziv} ({AktivnaTvar})" : Naziv;
}
