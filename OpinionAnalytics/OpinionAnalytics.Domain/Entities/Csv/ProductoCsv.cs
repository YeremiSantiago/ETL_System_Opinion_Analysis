using System;
using CsvHelper.Configuration.Attributes;

namespace OpinionAnalytics.Domain.Entities.Csv;

public class ProductoCsv
{
    [Name("IdProducto")]
    public int IdProducto { get; set; }

    [Name("Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Name("Categoría")]
    public string Categoria { get; set; } = string.Empty;
}