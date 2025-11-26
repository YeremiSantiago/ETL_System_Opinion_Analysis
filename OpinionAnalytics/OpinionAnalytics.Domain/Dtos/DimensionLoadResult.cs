using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpinionAnalytics.Domain.Dtos
{
    public class DimensionLoadResult
    {
        public int ClientesInserted { get; set; }
        public int ClientesUpdated { get; set; }
        public int ProductosInserted { get; set; }
        public int ProductosUpdated { get; set; }
        public int FuentesInserted { get; set; }
        public int FuentesUpdated { get; set; }
        public int SentimientosInserted { get; set; }
        public int SentimientosUpdated { get; set; }
        public int TiemposInserted { get; set; }
        public int TiemposUpdated { get; set; }

        public List<string> Errors { get; set; } = new();

    }
}
