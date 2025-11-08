using System;
using CsvHelper.Configuration.Attributes;

namespace OpinionAnalytics.Domain.Entities.Csv;

public class EncuestaInterna
{
    [Name("IdOpinion")]
    public int IdOpinion { get; set; }

    [Name("IdCliente")]
    public int IdCliente { get; set; }

    [Name("IdProducto")]
    public int IdProducto { get; set; }

    [Name("Fecha")]
    [Format("yyyy-MM-dd")]
    public DateTime Fecha { get; set; }

    [Name("Comentario")]
    public string Comentario { get; set; } = string.Empty;

    [Name("Clasificación")]
    public string Clasificacion { get; set; } = string.Empty;

    [Name("PuntajeSatisfacción")]
    public int PuntajeSatisfaccion { get; set; }

    [Name("Fuente")]
    public string Fuente { get; set; } = string.Empty;
}
