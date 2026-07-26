using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Configurations;

public sealed class ReportingProductConfiguration : IEntityTypeConfiguration<ReportingProduct>
{
    public void Configure(EntityTypeBuilder<ReportingProduct> builder)
    {
        builder.ToTable("ReportingProducts");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Name).HasMaxLength(200).IsRequired();
        builder.Property(product => product.Brand).HasMaxLength(100).IsRequired();
        builder.Property(product => product.Category).HasMaxLength(100).IsRequired();
        builder.Property(product => product.CreatedAtUtc).HasPrecision(0);
        builder.Property(product => product.UpdatedAtUtc).HasPrecision(0);
        builder.HasIndex(product => product.Name);
    }
}

public sealed class HistoricalProductMappingConfiguration : IEntityTypeConfiguration<HistoricalProductMapping>
{
    public void Configure(EntityTypeBuilder<HistoricalProductMapping> builder)
    {
        builder.ToTable("HistoricalProductMappings");
        builder.HasKey(mapping => mapping.Id);
        builder.Property(mapping => mapping.SourceSystem).HasMaxLength(100).IsRequired();
        builder.Property(mapping => mapping.ExternalProductKey).HasMaxLength(100).IsRequired();

        builder.HasIndex(mapping => new { mapping.SourceSystem, mapping.ExternalProductKey })
            .IsUnique();

        builder.HasIndex(mapping => new { mapping.ReportingProductId, mapping.SourceSystem })
            .IsUnique();

        builder.HasOne(mapping => mapping.ReportingProduct)
            .WithMany(product => product.HistoricalMappings)
            .HasForeignKey(mapping => mapping.ReportingProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LiveProductMappingConfiguration : IEntityTypeConfiguration<LiveProductMapping>
{
    public void Configure(EntityTypeBuilder<LiveProductMapping> builder)
    {
        builder.ToTable("LiveProductMappings");
        builder.HasKey(mapping => mapping.ReportingProductId);
        builder.Property(mapping => mapping.UseTransactionsFrom).HasColumnType("date");
        builder.Property(mapping => mapping.Reason).HasMaxLength(500);
        builder.Property(mapping => mapping.CreatedAtUtc).HasPrecision(0);
        builder.Property(mapping => mapping.UpdatedAtUtc).HasPrecision(0);
        builder.HasIndex(mapping => mapping.ProductId).IsUnique();

        builder.HasOne(mapping => mapping.ReportingProduct)
            .WithOne(product => product.LiveProductMapping)
            .HasForeignKey<LiveProductMapping>(mapping => mapping.ReportingProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mapping => mapping.Product)
            .WithOne()
            .HasForeignKey<LiveProductMapping>(mapping => mapping.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SalesImportBatchConfiguration : IEntityTypeConfiguration<SalesImportBatch>
{
    public void Configure(EntityTypeBuilder<SalesImportBatch> builder)
    {
        builder.ToTable("SalesImportBatches", table =>
        {
            table.HasCheckConstraint(
                "CK_SalesImportBatches_RowCounts",
                "[RowsRead] >= 0 AND [RowsInserted] >= 0 AND [RowsUpdated] >= 0 AND [ReportingProductsCreated] >= 0");
        });

        builder.HasKey(batch => batch.Id);
        builder.Property(batch => batch.SourceSystem).HasMaxLength(100).IsRequired();
        builder.Property(batch => batch.FileName).HasMaxLength(260).IsRequired();
        builder.Property(batch => batch.ContentSha256).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(batch => batch.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(batch => batch.ErrorMessage).HasMaxLength(2000);
        builder.Property(batch => batch.StartedAtUtc).HasPrecision(0);
        builder.Property(batch => batch.CompletedAtUtc).HasPrecision(0);

        builder.HasIndex(batch => new { batch.SourceSystem, batch.ContentSha256 })
            .IsUnique();
    }
}

public sealed class HistoricalMonthlyProductSaleConfiguration :
    IEntityTypeConfiguration<HistoricalMonthlyProductSale>
{
    public void Configure(EntityTypeBuilder<HistoricalMonthlyProductSale> builder)
    {
        builder.ToTable("HistoricalMonthlyProductSales", table =>
        {
            table.HasCheckConstraint("CK_HistoricalMonthlyProductSales_Year", "[Year] BETWEEN 1900 AND 9999");
            table.HasCheckConstraint("CK_HistoricalMonthlyProductSales_Month", "[Month] BETWEEN 1 AND 12");
            table.HasCheckConstraint("CK_HistoricalMonthlyProductSales_QuantitySold", "[QuantitySold] >= 0");
        });

        builder.HasKey(sale => sale.Id);
        builder.Property(sale => sale.Year).HasColumnType("smallint");
        builder.Property(sale => sale.Month).HasColumnType("tinyint");

        builder.HasIndex(sale => new { sale.ReportingProductId, sale.Year, sale.Month })
            .IsUnique();

        builder.HasOne(sale => sale.ReportingProduct)
            .WithMany(product => product.HistoricalMonthlySales)
            .HasForeignKey(sale => sale.ReportingProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sale => sale.SalesImportBatch)
            .WithMany(batch => batch.HistoricalMonthlySales)
            .HasForeignKey(sale => sale.SalesImportBatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
