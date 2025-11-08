using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpinionAnalytics.Domain.Entities.Db;

[Table("vw_WebReviewsForETL", Schema = "dbo")]
public class WebReviewView
{
    [Key]
    public int IdReview { get; set; }
    public int? IdCliente { get; set; }
    public int IdProducto { get; set; }
    public DateTime Fecha { get; set; }
    public string Comentario { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteEmail { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string ProductoCategoria { get; set; } = string.Empty;
    public string TipoFuente { get; set; } = string.Empty;
}
