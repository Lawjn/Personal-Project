using Microsoft.EntityFrameworkCore;
using StudentManagementAPI.Models;

namespace StudentManagementAPI
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Student> Students { get; set; }
        public DbSet<VehicleModel> VehicleModels { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<SellFromInventory> SellFromInventories { get; set; }
        public DbSet<ReportLog> ReportLogs { get; set; }
        public DbSet<Forecast> Forecasts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure VehicleModel
            modelBuilder.Entity<VehicleModel>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Price).HasPrecision(18, 2);
            });

            // Configure Inventory
            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                entity.HasOne(e => e.VehicleModel)
                      .WithMany(e => e.Inventories)
                      .HasForeignKey(e => e.VehicleModelId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure SellFromInventory
            modelBuilder.Entity<SellFromInventory>(entity =>
            {
                entity.Property(e => e.SellPrice).HasPrecision(18, 2);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                entity.HasOne(e => e.Inventory)
                      .WithMany(e => e.SalesRecords)
                      .HasForeignKey(e => e.InventoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ReportLog
            modelBuilder.Entity<ReportLog>(entity =>
            {
                entity.Property(e => e.TotalRevenue).HasPrecision(18, 2);
                entity.Property(e => e.TotalProfit).HasPrecision(18, 2);
            });

            // Configure Forecast
            modelBuilder.Entity<Forecast>(entity =>
            {
                entity.Property(e => e.PredictedRevenue).HasPrecision(18, 2);
                entity.Property(e => e.ConfidenceLevel).HasPrecision(5, 2);
                entity.HasOne(e => e.VehicleModel)
                      .WithMany()
                      .HasForeignKey(e => e.VehicleModelId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
