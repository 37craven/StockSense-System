using System.ComponentModel.DataAnnotations;
namespace StockSense.Domain.Entities;

public class OrderSlipItem
{
    public int Id { get; set; }
    public int OrderSlipId { get; set; } // Foreign Key linking back to OrderSlip
    // --- Product Snapshot Data ---
    // (We store these as strings/ints so if the original product is deleted, the historical receipt isn't ruined)
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int CurrentStock { get; set; }
    public int ReorderTarget { get; set; }
    // --- Core Order Data ---
    public int Quantity { get; set; } // The amount we are asking the supplier for
    public int ReceivedQuantity { get; set; } // The amount that actually arrived at the store
    // --- AI Intelligence Data ---
    public bool IsPredictedHighDemand { get; set; }
    public double ConfidenceScore { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public OrderSlip OrderSlip { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int CurrentStockSnapshot { get; set; }
    public int IncomingStockSnapshot { get; set; }
    public int ReservedStockSnapshot { get; set; }
    public int BackorderStockSnapshot { get; set; }
    public int InventoryPositionSnapshot { get; set; }
    public decimal AverageDailyDemandSnapshot { get; set; }
    public decimal LeadTimeDaysSnapshot { get; set; }
    public int SafetyStockSnapshot { get; set; }
    public int ReorderPointSnapshot { get; set; }
    public int TargetStockSnapshot { get; set; }
    public int SuggestedQuantity { get; set; }
    public int OrderedQuantity { get; set; }
    public int PackageSizeSnapshot { get; set; } = 1;
    public int MinimumOrderQuantitySnapshot { get; set; } = 1;
    public decimal UnitCostSnapshot { get; set; }
    public decimal EstimatedLineTotal { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;
    public ICollection<TransactionItem> ReceiptItems { get; set; } = new List<TransactionItem>();
}
