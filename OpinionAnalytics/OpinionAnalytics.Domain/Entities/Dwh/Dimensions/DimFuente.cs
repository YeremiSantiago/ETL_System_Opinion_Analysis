using OpinionAnalytics.Domain.Entities.Dwh.Facts;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace OpinionAnalytics.Domain.Entities.Dwh.Dimensions
{
    [Table("DimFuente", Schema = "Dimension")]
    public class DimFuente
    {
        [Key]
        [Column("FuenteKey")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FuenteKey { get; set; }

        [Required]
        [Column("IdFuente")]
        [StringLength(50)]
        public string IdFuente { get; set; } = string.Empty;

        [Column("TipoFuente")]
        [StringLength(100)]
        public string? TipoFuente { get; set; }

        [Column("NombreFuente")]
        [StringLength(100)]
        public string? NombreFuente { get; set; }

        [Column("FechaCarga")]
        public DateTime? FechaCarga { get; set; }

        // Navigation property
        public virtual ICollection<FactOpiniones> Opiniones { get; set; } = new List<FactOpiniones>();
    }
}
