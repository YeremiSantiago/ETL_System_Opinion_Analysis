using System;
using System.Collections.Generic;
using OpinionAnalytics.Domain.Entities.Dwh.Dimensions;

namespace OpinionAnalytics.Domain.Dtos
{
    public class DimensionLoadDto
    {
        public List<DimCliente> Clientes { get; set; } = new();
        public List<DimProducto> Productos { get; set; } = new();
        public List<DimFuente> Fuentes { get; set; } = new();
        public List<DimSentimiento> Sentimientos { get; set; } = new();
        public List<DimTiempo> Tiempos { get; set; } = new();
        
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public int TotalSourceRecords { get; set; }
        public List<string> ProcessedSources { get; set; } = new();
        public List<string> ValidationErrors { get; set; } = new();
    }
}
