using OpinionAnalytics.Domain.Entities.Dwh.Facts;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpinionAnalytics.Domain.Entities.Dwh.Dimensions;

[Table("DimCliente", Schema = "Dimension")]
public class DimCliente
{
    [Key]
    [Column("CLienteKey")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ClienteKey { get; set; }

    [Required]
    [Column("IdCliente")]
    [StringLength(50)]
    public string IdCliente { get; set; } = string.Empty;

    [Required]
    [Column("Nombre")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Column("Email")]
    [StringLength(51)]
    public string? Email { get; set; }

    // Navigation property
    public virtual ICollection<FactOpiniones> Opiniones { get; set; } = new List<FactOpiniones>();
}
