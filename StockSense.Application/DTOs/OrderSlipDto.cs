namespace StockSense.Application.DTOs;

// Changed to a class with { get; set; } so Blazor can edit the Quantity!
public class OrderSlipItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderTarget { get; set; }
    
    
    // Because this has a "set;", the red line in Blazor will disappear
    public int Quantity { get; set; } 
    public int ReceivedQuantity { get; set; }
    public int RemainingQuantity => Math.Max(0, OrderedQuantity - ReceivedQuantity);
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
}

// We change the parent to a class too, just in case you need to edit it later
public class OrderSlipDto
{
    public int Id { get; set; }
    public string SlipNumber { get; set; } = string.Empty;
    public DateTime DateGenerated { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierEmail { get; set; } = string.Empty;
    public bool IsReceived { get; set; }
    public string OrderSlipNumber { get; set; } = string.Empty;
    public string LocationId { get; set; } = "MAIN";
    public string Status { get; set; } = "Draft";
    public DateTime GeneratedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? OrderedAt { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? ApprovedByUserId { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public string? Remarks { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<OrderSlipItemDto> Items { get; set; } = new();
}
