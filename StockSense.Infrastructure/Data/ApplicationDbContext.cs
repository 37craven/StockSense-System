using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;


namespace StockSense.Infrastructure.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<StoreService> StoreServices { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<BuildRequest> BuildRequests { get; set; }
        public DbSet<OrderSlip> OrderSlips { get; set; }
        public DbSet<OrderSlipItem> OrderSlipItems { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Mechanic> Mechanics { get; set; }
        public DbSet<PreBuiltPackage> PreBuiltPackages { get; set; }
        public DbSet<SalesHistory> SalesHistory { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<PinnedSlip> PinnedSlips { get; set; }


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
