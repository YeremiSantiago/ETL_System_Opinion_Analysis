using Microsoft.EntityFrameworkCore;
using OpinionAnalytics.Domain.Entities.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpinionAnalytics.Persistence.Repositories.Db.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<WebReviewView> WebReviewsView { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<WebReviewView>()
                .ToView("vw_WebReviewsForETL")
                .HasNoKey(); 

            base.OnModelCreating(modelBuilder);
        }
    }
}
