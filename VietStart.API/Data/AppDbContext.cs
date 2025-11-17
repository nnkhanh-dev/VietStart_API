using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VietStart.API.Entities.Domains;

namespace VietStart.API.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<StartUp> StartUps { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<StartUpMedia> StartUpMedias { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<React> Reacts { get; set; }
        public DbSet<Share> Shares { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            // ======== SHARE (Fix cascade) ========
            modelBuilder.Entity<Share>(entity =>
            {
                // Composite key
                entity.HasKey(x => new { x.UserId, x.StartUpId });

                entity.HasOne(x => x.User)
                      .WithMany(x => x.Shares)
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Restrict);     // ⭐ Bắt buộc

                entity.HasOne(x => x.StartUp)
                      .WithMany(x => x.Shares)
                      .HasForeignKey(x => x.StartUpId)
                      .OnDelete(DeleteBehavior.Restrict);     // ⭐ Bắt buộc
            });

            // ======== COMMENT (bạn đã đúng) ========
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Nếu cần handle StartUp – Comment hoặc StartUp – React, hãy đảm bảo không Cascade
        }

    }
}
