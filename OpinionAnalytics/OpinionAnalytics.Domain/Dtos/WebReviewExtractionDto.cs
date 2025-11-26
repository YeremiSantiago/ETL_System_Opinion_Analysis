using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpinionAnalytics.Domain.Dtos
{
    public class WebReviewExtractionDto
    {
        public int IdProducto { get; set; }
        public string? NombreProducto { get; set; }
        public string? Categoria { get; set; }
        public string? Subcategoria { get; set; }
        public string? Usuario { get; set; }
        public string? Email { get; set; }
        public string? Sentimiento { get; set; }
        public DateTime Fecha { get; set; }

    }
}
