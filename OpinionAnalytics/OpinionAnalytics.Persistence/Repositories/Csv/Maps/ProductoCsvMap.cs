using CsvHelper.Configuration;
using OpinionAnalytics.Domain.Entities.Csv;

namespace OpinionAnalytics.Persistence.Repositories.Csv.Maps;

public class ProductoCsvMap : ClassMap<ProductoCsv>
{
    public ProductoCsvMap()
    {
        Map(m => m.IdProducto).Name("IdProducto");
        Map(m => m.Nombre).Name("Nombre");
        Map(m => m.Categoria).Name("Categoría");
    }
}