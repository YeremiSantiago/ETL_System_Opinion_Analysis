using Microsoft.EntityFrameworkCore;
using OpinionAnalytics.Domain.Entities.Api;

namespace OpinionAnalytics.Api.Data.Context
{
    public class ApiDbContext : DbContext
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options) { }


        public DbSet<SocialCommentView> SocialCommentsView { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<SocialCommentView>()
                .ToView("vw_SocialCommentsForETL")
                .HasKey(x => x.IdComment);

            base.OnModelCreating(modelBuilder);
        }
    }
}
