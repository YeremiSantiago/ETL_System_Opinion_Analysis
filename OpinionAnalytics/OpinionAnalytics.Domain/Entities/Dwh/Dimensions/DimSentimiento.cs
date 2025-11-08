using OpinionAnalytics.Domain.Entities.Dwh.Facts;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpinionAnalytics.Domain.Entities.Dwh.Dimensions;

[Table("DimSentimiento", Schema = "Dimension")]
public class DimSentimiento
{
    [Key]
    [Column("SentimientoKey")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SentimientoKey { get; set; }

    [Required]
    [Column("Clasificacion")]
    [StringLength(50)]
    public string Clasificacion { get; set; } = string.Empty;

    [Column("Descripcion")]
    [StringLength(200)]
    public string? Descripcion { get; set; }

    [Required]
    [Column("ValorNumerico")]
    public int ValorNumerico { get; set; }

    // Navigation property
    public virtual ICollection<FactOpiniones> Opiniones { get; set; } = new List<FactOpiniones>();
}
