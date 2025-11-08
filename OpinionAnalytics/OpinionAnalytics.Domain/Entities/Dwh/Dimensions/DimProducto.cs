using OpinionAnalytics.Domain.Entities.Dwh.Facts;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpinionAnalytics.Domain.Entities.Dwh.Dimensions;

[Table("DimProducto", Schema = "Dimension")]
public class DimProducto
{
    [Key]
    [Column("ProductoKey")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ProductoKey { get; set; }

    [Required]
    [Column("IdProducto")]
    [StringLength(50)]
    public string IdProducto { get; set; } = string.Empty;

    [Required]
    [Column("NombreProducto")]
    [StringLength(200)]
    public string NombreProducto { get; set; } = string.Empty;

    [Column("Categoria")]
    [StringLength(100)]
    public string? Categoria { get; set; }

    [Column("Subcategoria")]
    [StringLength(100)]
    public string? Subcategoria { get; set; }

    // Navigation property
    public virtual ICollection<FactOpiniones> Opiniones { get; set; } = new List<FactOpiniones>();
}
