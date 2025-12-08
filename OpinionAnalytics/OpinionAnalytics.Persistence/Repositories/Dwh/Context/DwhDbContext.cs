using Microsoft.EntityFrameworkCore;
using OpinionAnalytics.Domain.Entities.Dwh.Dimensions;
using OpinionAnalytics.Domain.Entities.Dwh.Facts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpinionAnalytics.Persistence.Repositories.Dwh.Context
{
    public class DwhDbContext : DbContext
    {
        public DwhDbContext(DbContextOptions<DwhDbContext> options) : base(options) { }

        public DbSet<DimCliente> DimClientes { get; set; }
        public DbSet<DimFuente> DimFuentes { get; set; }
        public DbSet<DimProducto> DimProductos { get; set; }
        public DbSet<DimSentimiento> DimSentimientos { get; set; }
        public DbSet<DimTiempo> DimTiempos { get; set; }

        
        public DbSet<FactOpiniones> FactOpiniones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

           
        }
    }
}
