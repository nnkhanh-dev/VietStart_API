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
        public DbSet<TeamStartUp> TeamStartUps { get; set; }
        public DbSet<Position> Positions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Share composite key
            modelBuilder.Entity<Share>()
                .HasKey(s => new { s.UserId, s.StartUpId });

            // Configure AppUser relationships
            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.StartUps)
                .WithOne(s => s.AppUser)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Comments)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Shares)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure StartUp relationships
            modelBuilder.Entity<StartUp>()
                .HasMany(s => s.Comments)
                .WithOne(c => c.StartUp)
                .HasForeignKey(c => c.StartUpId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StartUp>()
                .HasMany(s => s.StartUpMedias)
                .WithOne(m => m.StartUp)
                .HasForeignKey(m => m.StartUpId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StartUp>()
                .HasMany(s => s.Shares)
                .WithOne(sh => sh.StartUp)
                .HasForeignKey(sh => sh.StartUpId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StartUp>()
                .HasOne(s => s.Category)
                .WithMany(c => c.StartUps)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Comment self-referencing relationship
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure React relationships
            modelBuilder.Entity<React>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<React>()
                .HasOne(r => r.Comment)
                .WithMany(c => c.Reacts)
                .HasForeignKey(r => r.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<React>()
                .HasOne(r => r.StartUp)
                .WithMany(s => s.Reacts)
                .HasForeignKey(r => r.StartUpId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure TeamStartUp relationships
            modelBuilder.Entity<TeamStartUp>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TeamStartUp>()
                .HasOne(t => t.StartUp)
                .WithMany()
                .HasForeignKey(t => t.StartUpId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TeamStartUp>()
                .HasOne(t => t.Position)
                .WithMany()
                .HasForeignKey(t => t.PositionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
