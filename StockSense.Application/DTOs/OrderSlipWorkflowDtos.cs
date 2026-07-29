namespace StockSense.Application.DTOs;

public sealed class OrderSlipGenerationRequest
{
    public string LocationId { get; set; } = "MAIN";
    public IReadOnlyCollection<int>? ProductIds { get; set; }
}

public sealed class OrderSlipPreviewDto
{
    public string LocationId { get; set; } = "MAIN";
    public DateTime GeneratedAt { get; set; }
    public List<OrderSlipPreviewGroupDto> SupplierGroups { get; set; } = new();
    public List<OrderSlipGenerationWarningDto> Warnings { get; set; } = new();
}

public sealed class OrderSlipPreviewGroupDto
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalEstimatedCost => Items.Sum(item => item.EstimatedLineTotal);
    public List<OrderSlipPreviewItemDto> Items { get; set; } = new();
}

public sealed class OrderSlipPreviewItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int IncomingStock { get; set; }
    public int ReservedStock { get; set; }
    public int BackorderStock { get; set; }
    public int InventoryPosition { get; set; }
    public decimal AverageDailyDemand { get; set; }
    public decimal LeadTimeDays { get; set; }
    public int SafetyStock { get; set; }
    public int ReorderPoint { get; set; }
    public int TargetStock { get; set; }
    public int SuggestedQuantity { get; set; }
    public int FinalQuantity { get; set; }
    public int PackageSize { get; set; } = 1;
    public int MinimumOrderQuantity { get; set; } = 1;
    public int? MaximumStockLevel { get; set; }
    public decimal UnitCost { get; set; }
    public decimal EstimatedLineTotal { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;
    public bool IsSelected { get; set; } = true;
}

public sealed record OrderSlipGenerationWarningDto(
    int ProductId,
    string ProductName,
    string Code,
    string Message);

public sealed class CreateOrderSlipDraftsCommand
{
    public string LocationId { get; set; } = "MAIN";
    public string? CreatedByUserId { get; set; }
    public string? Remarks { get; set; }
    public List<CreateDraftOrderSlipGroupCommand> SupplierGroups { get; set; } = new();
}

public sealed class CreateDraftOrderSlipGroupCommand
{
    public int SupplierId { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public List<CreateDraftOrderSlipItemCommand> Items { get; set; } = new();
}

public sealed class CreateDraftOrderSlipItemCommand
{
    public int ProductId { get; set; }
    public int OrderedQuantity { get; set; }
}

public sealed record CreateDraftOrderSlipsResult(
    IReadOnlyList<OrderSlipDto> OrderSlips,
    IReadOnlyList<OrderSlipGenerationWarningDto> Warnings);

public sealed class ManualOrderSlipCatalogDto
{
    public List<ManualOrderSlipSupplierDto> Suppliers { get; set; } = new();
    public List<ManualOrderSlipProductDto> Products { get; set; } = new();
}

public sealed class ManualOrderSlipSupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class ManualOrderSlipProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public decimal UnitCost { get; set; }
    public int CurrentStock { get; set; }
    public int MinimumOrderQuantity { get; set; } = 1;
    public int PackageSize { get; set; } = 1;
    public int? MaximumStockLevel { get; set; }
    public bool HasInventorySettings { get; set; }
}

public sealed class CreateManualOrderSlipDraftCommand
{
    public string LocationId { get; set; } = "MAIN";
    public string? CreatedByUserId { get; set; }
    public int SupplierId { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<CreateManualOrderSlipItemCommand> Items { get; set; } = new();
}

public sealed class CreateManualOrderSlipItemCommand
{
    public int ProductId { get; set; }
    public int OrderedQuantity { get; set; }
}

public sealed class OrderSlipTransitionCommand
{
    public int OrderSlipId { get; set; }
    public string TargetStatus { get; set; } = string.Empty;
    public string? ActingUserId { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? Remarks { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class CancelOrderSlipCommand
{
    public int OrderSlipId { get; set; }
    public string? ActingUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ReceiveOrderSlipCommand
{
    public int OrderSlipId { get; set; }
    public string LocationId { get; set; } = "MAIN";
    public DateTime ReceivedAt { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Remarks { get; set; }
    public string? ReceivedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<ReceiveOrderSlipItemCommand> Items { get; set; } = new();
}

public sealed class ReceiveOrderSlipItemCommand
{
    public int OrderSlipItemId { get; set; }
    public int QuantityReceived { get; set; }
}

public sealed record OrderSlipReceiptResult(
    int OrderSlipId,
    int TransactionId,
    string TransactionNumber,
    string OrderSlipStatus,
    IReadOnlyList<int> AffectedProductIds);
