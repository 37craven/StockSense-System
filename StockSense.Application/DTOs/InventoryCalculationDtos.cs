namespace StockSense.Application.DTOs;

public sealed record SafetyStockCalculationResult(
    int ProductId,
    string ProductName,
    string LocationId,
    string CalculationStage,
    decimal AverageDailyDemand,
    decimal DemandStandardDeviation,
    decimal AverageLeadTimeDays,
    decimal LeadTimeStandardDeviation,
    int SafetyStock,
    int ReorderPoint,
    int TargetStock,
    int UsableDataDays,
    int TotalObservedDemand,
    string ConfidenceLevel,
    decimal ServiceLevel,
    decimal ZScore,
    string Explanation,
    bool ManualOverrideUsed,
    DateTime CalculatedAt,
    string CalculationVersion);

public sealed class ProductInventorySettingDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string LocationId { get; set; } = "MAIN";
    public string CalculationMode { get; set; } = "Auto";
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
    public byte[] RowVersion { get; set; } = [];
}
