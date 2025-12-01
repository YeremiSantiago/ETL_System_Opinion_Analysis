using CsvHelper.Configuration;
using OpinionAnalytics.Domain.Entities.Csv;

namespace OpinionAnalytics.Persistence.Repositories.Csv.Maps;

public class ClienteCsvMap : ClassMap<ClienteCsv>
{
    public ClienteCsvMap()
    {
        Map(m => m.IdCliente).Name("IdCliente");
        Map(m => m.Nombre).Name("Nombre");
        Map(m => m.Email).Name("Email");
        Map(m => m.Telefono).Name("Telefono").Optional();
        Map(m => m.Ciudad).Name("Ciudad").Optional();
        Map(m => m.FechaRegistro).Name("FechaRegistro").Optional();
    }
}