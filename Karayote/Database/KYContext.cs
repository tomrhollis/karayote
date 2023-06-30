using Karayote.Models;
using Microsoft.EntityFrameworkCore;

namespace Karayote.Database
{
    internal class KYContext : DbContext
    {
        public KYContext(DbContextOptions<KYContext> options) : base(options) { }

        // list of tables (classes)
        public DbSet<KarayoteUser> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
