using OpinionAnalytics.Domain.Entities.Dwh.Facts;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpinionAnalytics.Domain.Entities.Dwh.Dimensions
{
    [Table("DimTiempo", Schema = "Dimension")]
    public class DimTiempo
    {
        [Key]
        [Column("TiempoKey")]
        public int TiempoKey { get; set; }

        [Required]
        [Column("Fecha")]
        public DateTime Fecha { get; set; }

        [Required]
        [Column("Anio")]
        public int Anio { get; set; }

        [Required]
        [Column("Trimestre")]
        public int Trimestre { get; set; }

        [Column("MesNombre")]
        [StringLength(50)]
        public string? MesNombre { get; set; }

        [Column("Semana")]
        public int? Semana { get; set; }

        [Required]
        [Column("Dia")]
        public int Dia { get; set; }

        [Required]
        [Column("Mes")]
        public int Mes { get; set; }

        // Navigation property
        public virtual ICollection<FactOpiniones> Opiniones { get; set; } = new List<FactOpiniones>();
    }
}
