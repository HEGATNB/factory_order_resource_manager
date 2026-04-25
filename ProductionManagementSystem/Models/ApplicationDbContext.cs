using Microsoft.EntityFrameworkCore;

namespace ProductionManagementSystem.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductionLine> ProductionLines { get; set; } = null!;
        public DbSet<Material> Materials { get; set; } = null!;
        public DbSet<ProductMaterial> ProductMaterials { get; set; } = null!;
        public DbSet<WorkOrder> WorkOrders { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Составной ключ для ProductMaterial
            modelBuilder.Entity<ProductMaterial>()
                .HasKey(pm => new { pm.ProductId, pm.MaterialId });

            // Связь ProductMaterial -> Product
            modelBuilder.Entity<ProductMaterial>()
                .HasOne(pm => pm.Product)
                .WithMany(p => p.ProductMaterials)
                .HasForeignKey(pm => pm.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Связь ProductMaterial -> Material
            modelBuilder.Entity<ProductMaterial>()
                .HasOne(pm => pm.Material)
                .WithMany(m => m.ProductMaterials)
                .HasForeignKey(pm => pm.MaterialId)
                .OnDelete(DeleteBehavior.Cascade);

            // Связь WorkOrder -> Product
            modelBuilder.Entity<WorkOrder>()
                .HasOne(w => w.Product)
                .WithMany(p => p.WorkOrders)
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Связь WorkOrder -> ProductionLine
            modelBuilder.Entity<WorkOrder>()
                .HasOne(w => w.ProductionLine)
                .WithMany(l => l.WorkOrders)
                .HasForeignKey(w => w.ProductionLineId)
                .OnDelete(DeleteBehavior.SetNull);

            // Связь ProductionLine -> CurrentWorkOrder
            modelBuilder.Entity<ProductionLine>()
                .HasOne(l => l.CurrentWorkOrder)
                .WithMany()
                .HasForeignKey(l => l.CurrentWorkOrderId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}