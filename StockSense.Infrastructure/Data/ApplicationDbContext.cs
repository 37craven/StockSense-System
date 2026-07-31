using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

// ponytail: docker run -d --hostname=stocksense-db --name stocksense-db -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=YourPassword123! -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest

namespace StockSense.Infrastructure.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<StoreService> StoreServices { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductInventorySetting> ProductInventorySettings { get; set; }
        public DbSet<ProductInventoryMetric> ProductInventoryMetrics { get; set; }
        public DbSet<BuildRequest> BuildRequests { get; set; }
        public DbSet<OrderSlip> OrderSlips { get; set; }
        public DbSet<OrderSlipItem> OrderSlipItems { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Mechanic> Mechanics { get; set; }
        public DbSet<PreBuiltPackage> PreBuiltPackages { get; set; }
        public DbSet<Motorcycle> Motorcycles { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionItem> TransactionItems { get; set; }
        public DbSet<PinnedSlip> PinnedSlips { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            builder.Entity<PreBuiltPackageMotor>()
                .HasOne(m => m.Motorcycle)
                .WithMany()
                .HasForeignKey(m => m.MotorcycleId)
                .OnDelete(DeleteBehavior.Restrict);
        }


        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 1. Define Philippine Time globally for the database
            var phZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var phNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phZone);

            // 2. Look at every single row that is about to be Added or Updated
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                // If the table has a "CreatedAt" column and it's a new row, force PH Time
                if (entry.State == EntityState.Added)
                {
                    var createdAtProp = entry.Entity.GetType().GetProperty("CreatedAt");
                    if (createdAtProp != null && createdAtProp.PropertyType == typeof(DateTime))
                    {
                        createdAtProp.SetValue(entry.Entity, phNow);
                    }
                }

                // If the table has an "UpdatedAt" column, force PH Time
                var updatedAtProp = entry.Entity.GetType().GetProperty("UpdatedAt");
                if (updatedAtProp != null && updatedAtProp.PropertyType == typeof(DateTime))
                {
                    updatedAtProp.SetValue(entry.Entity, phNow);
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }



}
