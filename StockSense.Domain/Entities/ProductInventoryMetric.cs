using System.ComponentModel.DataAnnotations;

namespace StockSense.Domain.Entities;

public class ProductInventoryMetric
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string LocationId { get; set; } = InventoryDefaults.LocationId;
    public decimal AverageDailyDemand { get; set; }
    public decimal DemandStandardDeviation { get; set; }
    public decimal AverageLeadTimeDays { get; set; }
    public decimal LeadTimeStandardDeviation { get; set; }
    public int SafetyStock { get; set; }
    public int TargetStock { get; set; }
    public int UsableDataDays { get; set; }
    public int TotalObservedDemand { get; set; }
    public string CalculationStage { get; set; } = InventoryCalculationStages.ColdStart;
    public string ConfidenceLevel { get; set; } = InventoryConfidenceLevels.Low;
    public string? CalculationReason { get; set; }
    public DateTime LastCalculatedAt { get; set; }
    public string CalculationVersion { get; set; } = InventoryDefaults.CalculationVersion;
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
