using System.ComponentModel.DataAnnotations;

namespace StockSense.Domain.Entities;

public class ProductInventorySetting
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string LocationId { get; set; } = InventoryDefaults.LocationId;
    public string CalculationMode { get; set; } = InventoryCalculationModes.Auto;
    public decimal InitialEstimatedWeeklyDemand { get; set; }
    public int DefaultLeadTimeDays { get; set; } = 7;
    public int ReviewPeriodDays { get; set; } = 7;
    public int BufferDays { get; set; } = 7;
    public decimal ServiceLevel { get; set; } = 0.9500m;
    public int MinimumSafetyStock { get; set; }
    public int? MaximumSafetyStock { get; set; }
    public int MinimumOrderQuantity { get; set; } = 1;
    public int PackageSize { get; set; } = 1;
    public int? MaximumStockLevel { get; set; }
    public int? ManualSafetyStock { get; set; }
    public int? ManualReorderPoint { get; set; }
    public bool IsAutomaticOrderEnabled { get; set; } = true;
    public DateTime InventoryTrackingStartDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
