using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpinionAnalytics.Domain.Entities.Dwh.Dimensions;

namespace OpinionAnalytics.Domain.Entities.Dwh.Facts;

[Table("FactOpiniones", Schema = "Fact")]
public class FactOpiniones
{
    [Key]
    [Column("OpinionKey")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long OpinionKey { get; set; }

    [Required]
    [Column("ClienteKey")]
    public int ClienteKey { get; set; }

    [Required]
    [Column("ProductoKey")]
    public int ProductoKey { get; set; }

    [Required]
    [Column("FuenteKey")]
    public int FuenteKey { get; set; }

    [Required]
    [Column("TiempoKey")]
    public int TiempoKey { get; set; }

    [Required]
    [Column("SentimientoKey")]
    public int SentimientoKey { get; set; }

    [Required]
    [Column("Calificacion", TypeName = "decimal(4,2)")]
    public decimal Calificacion { get; set; }

    // Navigation properties
    [ForeignKey("ClienteKey")]
    public virtual DimCliente Cliente { get; set; } = null!;

    [ForeignKey("ProductoKey")]
    public virtual DimProducto Producto { get; set; } = null!;

    [ForeignKey("FuenteKey")]
    public virtual DimFuente Fuente { get; set; } = null!;

    [ForeignKey("TiempoKey")]
    public virtual DimTiempo Tiempo { get; set; } = null!;

    [ForeignKey("SentimientoKey")]
    public virtual DimSentimiento Sentimiento { get; set; } = null!;
}
