using Microsoft.EntityFrameworkCore;
using TouristGuide.Models;

namespace TouristGuide.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<City> Cities { get; set; }
        public DbSet<Attraction> Attractions { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Настройка отношений
            modelBuilder.Entity<City>()
                .HasMany(c => c.Attractions)
                .WithOne(a => a.City)
                .HasForeignKey(a => a.CityId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}