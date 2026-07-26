namespace StockSense.Application.DTOs;

public sealed class InventoryDashboardRowDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int? SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int IncomingStock { get; set; }
    public int InventoryPosition { get; set; }
    public decimal AverageDailyDemand { get; set; }
    public decimal DemandStandardDeviation { get; set; }
    public int SafetyStock { get; set; }
    public int ReorderPoint { get; set; }
    public int TargetStock { get; set; }
    public string CalculationStage { get; set; } = "Not calculated";
    public string ConfidenceLevel { get; set; } = "Low";
    public DateTime? LastCalculatedAt { get; set; }
    public string CalculationExplanation { get; set; } = string.Empty;
    public bool IsAutomaticOrderEnabled { get; set; }
    public string CalculationMode { get; set; } = "Auto";
}

public sealed record InventoryRecalculationSummaryDto(
    int RequestedCount,
    int CompletedCount,
    IReadOnlyList<SafetyStockCalculationResult> Results);
