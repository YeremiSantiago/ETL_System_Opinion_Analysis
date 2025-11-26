using CsvHelper.Configuration;
using OpinionAnalytics.Domain.Entities.Csv;

public sealed class EncuestaInternaMap : ClassMap<EncuestaInterna>
{
    public EncuestaInternaMap()
    {
        Map(m => m.IdOpinion).Name("IdOpinion", "id_opinion", "idOpinion");
        Map(m => m.IdCliente).Name("IdCliente", "id_cliente", "idCliente");
        Map(m => m.IdProducto).Name("IdProducto", "id_producto", "idProducto");
        Map(m => m.Fecha).Name("Fecha", "fecha");
        Map(m => m.Comentario).Name("Comentario", "comentario");
        Map(m => m.Clasificacion).Name("Clasificación", "Clasificacion", "clasificacion");
        Map(m => m.PuntajeSatisfaccion).Name("PuntajeSatisfacción", "PuntajeSatisfaccion", "puntaje_satisfaccion");
        Map(m => m.Fuente).Name("Fuente", "fuente");
    }
}