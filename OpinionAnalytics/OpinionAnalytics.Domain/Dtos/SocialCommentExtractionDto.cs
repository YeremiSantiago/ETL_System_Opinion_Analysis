using System;

namespace OpinionAnalytics.Domain.Dtos
{
    public class SocialCommentExtractionDto
    {
        public int IdComment { get; set; }
        public int? IdCliente { get; set; }
        public int IdProducto { get; set; }
        public string Fuente { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Comentario { get; set; } = string.Empty;
        public string? ClienteNombre { get; set; }
        public string? ClienteEmail { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public string ProductoCategoria { get; set; } = string.Empty;
        public string TipoFuente { get; set; } = string.Empty;
    }
}
