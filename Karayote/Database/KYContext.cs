using Karayote.Models;
using Microsoft.EntityFrameworkCore;

namespace Karayote.Database
{
    internal class KYContext : DbContext
    {
        public KYContext(DbContextOptions<KYContext> options) : base(options) { }

        // list of tables (classes)
        public DbSet<Session> Sessions { get; set; }
        public DbSet<SongQueue> SongQueues { get; set; }
        public DbSet<KarayoteUser> Users { get; set; }
        public DbSet<SelectedSong> Songs { get; set; }
      

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KarafunSong>();
            modelBuilder.Entity<YoutubeSong>();
            modelBuilder.Entity<PlaceholderSong>();

            modelBuilder.Entity<KarayoteUser>().HasKey(x=> x.Id);

            modelBuilder.Entity<SelectedSong>().HasKey(x=> x.Id);
            base.OnModelCreating(modelBuilder);
        }
    }
}
