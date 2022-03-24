using Karayote.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Karayote.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        DbSet<SongRequest> SongRequests { get; set; }
        DbSet<Song> Songs { get; set; } // only the songs that have been encountered before

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<SongRequest>()
                .HasKey(r => new { r.SongId, r.UserId });
            builder.Entity<SongRequest>()
                .HasOne(r => r.User)
                .WithMany(u => u.SongRequests)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<SongRequest>()
                .HasOne(r => r.Song)
                .WithMany(s => s.SongRequests)
                .HasForeignKey(r => r.SongId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}