using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Configurations;

public sealed class ProductInventorySettingConfiguration : IEntityTypeConfiguration<ProductInventorySetting>
{
    public void Configure(EntityTypeBuilder<ProductInventorySetting> builder)
    {
        builder.ToTable("ProductInventorySettings", table =>
        {
            table.HasCheckConstraint("CK_ProductInventorySettings_Demand", "[InitialEstimatedWeeklyDemand] >= 0");
            table.HasCheckConstraint("CK_ProductInventorySettings_LeadReviewBuffer", "[DefaultLeadTimeDays] >= 1 AND [ReviewPeriodDays] >= 1 AND [BufferDays] >= 0");
            table.HasCheckConstraint("CK_ProductInventorySettings_ServiceLevel", "[ServiceLevel] >= 0.5000 AND [ServiceLevel] <= 0.9990");
            table.HasCheckConstraint("CK_ProductInventorySettings_SafetyLimits", "[MinimumSafetyStock] >= 0 AND ([MaximumSafetyStock] IS NULL OR [MaximumSafetyStock] >= [MinimumSafetyStock])");
            table.HasCheckConstraint("CK_ProductInventorySettings_OrderRules", "[MinimumOrderQuantity] >= 1 AND [PackageSize] >= 1 AND ([MaximumStockLevel] IS NULL OR [MaximumStockLevel] > 0)");
            table.HasCheckConstraint("CK_ProductInventorySettings_ManualValues", "([ManualSafetyStock] IS NULL OR [ManualSafetyStock] >= 0) AND ([ManualReorderPoint] IS NULL OR [ManualReorderPoint] >= 0)");
            table.HasCheckConstraint("CK_ProductInventorySettings_Mode", "[CalculationMode] IN ('Auto', 'Manual')");
        });

        builder.Property(setting => setting.LocationId).HasMaxLength(50).HasDefaultValue(InventoryDefaults.LocationId).IsRequired();
        builder.Property(setting => setting.CalculationMode).HasMaxLength(20).HasDefaultValue(InventoryCalculationModes.Auto).IsRequired();
        builder.Property(setting => setting.InitialEstimatedWeeklyDemand).HasPrecision(18, 4).HasDefaultValue(0m);
        builder.Property(setting => setting.ServiceLevel).HasPrecision(6, 4).HasDefaultValue(0.9500m);
        builder.Property(setting => setting.DefaultLeadTimeDays).HasDefaultValue(7);
        builder.Property(setting => setting.ReviewPeriodDays).HasDefaultValue(7);
        builder.Property(setting => setting.BufferDays).HasDefaultValue(7);
        builder.Property(setting => setting.MinimumSafetyStock).HasDefaultValue(0);
        builder.Property(setting => setting.MinimumOrderQuantity).HasDefaultValue(1);
        builder.Property(setting => setting.PackageSize).HasDefaultValue(1);
        builder.Property(setting => setting.IsAutomaticOrderEnabled).HasDefaultValue(true);
        builder.Property(setting => setting.RowVersion).IsRowVersion();
        builder.HasIndex(setting => new { setting.ProductId, setting.LocationId }).IsUnique();
        builder.HasOne(setting => setting.Product)
            .WithMany(product => product.InventorySettings)
            .HasForeignKey(setting => setting.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductInventoryMetricConfiguration : IEntityTypeConfiguration<ProductInventoryMetric>
{
    public void Configure(EntityTypeBuilder<ProductInventoryMetric> builder)
    {
        builder.ToTable("ProductInventoryMetrics", table =>
        {
            table.HasCheckConstraint("CK_ProductInventoryMetrics_Demand", "[AverageDailyDemand] >= 0 AND [DemandStandardDeviation] >= 0 AND [TotalObservedDemand] >= 0 AND [UsableDataDays] >= 0");
            table.HasCheckConstraint("CK_ProductInventoryMetrics_LeadTime", "[AverageLeadTimeDays] >= 0 AND [LeadTimeStandardDeviation] >= 0");
            table.HasCheckConstraint("CK_ProductInventoryMetrics_Stock", "[SafetyStock] >= 0 AND [TargetStock] >= 0");
            table.HasCheckConstraint("CK_ProductInventoryMetrics_Stage", "[CalculationStage] IN ('ColdStart','Learning','DataDriven','Manual')");
            table.HasCheckConstraint("CK_ProductInventoryMetrics_Confidence", "[ConfidenceLevel] IN ('Low','Medium','High')");
        });

        builder.Property(metric => metric.LocationId).HasMaxLength(50).IsRequired();
        builder.Property(metric => metric.AverageDailyDemand).HasPrecision(18, 4).HasDefaultValue(0m);
        builder.Property(metric => metric.DemandStandardDeviation).HasPrecision(18, 4).HasDefaultValue(0m);
        builder.Property(metric => metric.AverageLeadTimeDays).HasPrecision(18, 4).HasDefaultValue(0m);
        builder.Property(metric => metric.LeadTimeStandardDeviation).HasPrecision(18, 4).HasDefaultValue(0m);
        builder.Property(metric => metric.SafetyStock).HasDefaultValue(0);
        builder.Property(metric => metric.TargetStock).HasDefaultValue(0);
        builder.Property(metric => metric.TotalObservedDemand).HasDefaultValue(0);
        builder.Property(metric => metric.UsableDataDays).HasDefaultValue(0);
        builder.Property(metric => metric.CalculationStage).HasMaxLength(30).IsRequired();
        builder.Property(metric => metric.ConfidenceLevel).HasMaxLength(20).IsRequired();
        builder.Property(metric => metric.CalculationReason).HasMaxLength(500);
        builder.Property(metric => metric.CalculationVersion).HasMaxLength(20).IsRequired();
        builder.Property(metric => metric.RowVersion).IsRowVersion();
        builder.HasIndex(metric => new { metric.ProductId, metric.LocationId }).IsUnique();
        builder.HasOne(metric => metric.Product)
            .WithMany(product => product.InventoryMetrics)
            .HasForeignKey(metric => metric.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OrderSlipConfiguration : IEntityTypeConfiguration<OrderSlip>
{
    public void Configure(EntityTypeBuilder<OrderSlip> builder)
    {
        builder.ToTable("OrderSlips", table =>
        {
            table.HasCheckConstraint("CK_OrderSlips_TotalEstimatedCost", "[TotalEstimatedCost] >= 0");
            table.HasCheckConstraint("CK_OrderSlips_Status", "[Status] IN ('Draft','Approved','Ordered','PartiallyReceived','Completed','Cancelled')");
        });
        builder.Property(slip => slip.OrderSlipNumber).HasMaxLength(80).IsRequired();
        builder.Property(slip => slip.LocationId).HasMaxLength(50).HasDefaultValue(InventoryDefaults.LocationId).IsRequired();
        builder.Property(slip => slip.Status).HasMaxLength(30).HasDefaultValue(OrderSlipStatuses.Draft).IsRequired();
        builder.Property(slip => slip.CreatedByUserId).HasMaxLength(450);
        builder.Property(slip => slip.ApprovedByUserId).HasMaxLength(450);
        builder.Property(slip => slip.TotalEstimatedCost).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(slip => slip.Remarks).HasMaxLength(500);
        builder.Property(slip => slip.RowVersion).IsRowVersion();
        builder.HasIndex(slip => slip.OrderSlipNumber).IsUnique();
        builder.HasIndex(slip => new { slip.SupplierId, slip.LocationId, slip.Status });
        builder.HasOne(slip => slip.Supplier).WithMany().HasForeignKey(slip => slip.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(slip => slip.Items).WithOne(item => item.OrderSlip).HasForeignKey(item => item.OrderSlipId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OrderSlipItemConfiguration : IEntityTypeConfiguration<OrderSlipItem>
{
    public void Configure(EntityTypeBuilder<OrderSlipItem> builder)
    {
        builder.ToTable("OrderSlipItems", table =>
        {
            table.HasCheckConstraint("CK_OrderSlipItems_Quantities", "[OrderedQuantity] > 0 AND [SuggestedQuantity] >= 0 AND [ReceivedQuantity] >= 0 AND [ReceivedQuantity] <= [OrderedQuantity]");
            table.HasCheckConstraint("CK_OrderSlipItems_OrderRules", "[PackageSizeSnapshot] >= 1 AND [MinimumOrderQuantitySnapshot] >= 1");
            table.HasCheckConstraint("CK_OrderSlipItems_StockSnapshots", "[CurrentStockSnapshot] >= 0 AND [IncomingStockSnapshot] >= 0 AND [ReservedStockSnapshot] >= 0 AND [BackorderStockSnapshot] >= 0");
            table.HasCheckConstraint("CK_OrderSlipItems_Amounts", "[UnitCostSnapshot] >= 0 AND [EstimatedLineTotal] >= 0");
        });
        builder.Property(item => item.AverageDailyDemandSnapshot).HasPrecision(18, 4);
        builder.Property(item => item.LeadTimeDaysSnapshot).HasPrecision(18, 4);
        builder.Property(item => item.UnitCostSnapshot).HasPrecision(18, 2);
        builder.Property(item => item.EstimatedLineTotal).HasPrecision(18, 2);
        builder.Property(item => item.RecommendationReason).HasMaxLength(500).IsRequired();
        builder.HasIndex(item => item.ProductId);
        builder.HasIndex(item => new { item.OrderSlipId, item.ProductId }).IsUnique();
        builder.HasOne(item => item.Product).WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
