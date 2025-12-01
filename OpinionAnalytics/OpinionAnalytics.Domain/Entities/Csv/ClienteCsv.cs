using System;
using CsvHelper.Configuration.Attributes;

namespace OpinionAnalytics.Domain.Entities.Csv;

public class ClienteCsv
{
    [Name("IdCliente")]
    public int IdCliente { get; set; }

    [Name("Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Name("Email")]
    public string Email { get; set; } = string.Empty;

    [Name("Telefono")]
    public string? Telefono { get; set; }

    [Name("Ciudad")]
    public string? Ciudad { get; set; }

    [Name("FechaRegistro")]
    [Format("yyyy-MM-dd")]
    public DateTime? FechaRegistro { get; set; }
}