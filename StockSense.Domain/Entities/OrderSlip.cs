namespace StockSense.Domain.Entities;

public class OrderSlip
{
    public int Id { get; set; }
    // Legacy fields remain for existing UI and stored records.
    public string SlipNumber { get; set; } = string.Empty;
    public DateTime DateGenerated { get; set; } = DateTime.Now;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public List<OrderSlipItem> Items { get; set; } = new();
    public bool IsReceived { get; set; }
    public string OrderSlipNumber { get; set; } = string.Empty;
    public string LocationId { get; set; } = InventoryDefaults.LocationId;
    public string Status { get; set; } = OrderSlipStatuses.Draft;
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
    public ICollection<Transaction> PurchaseReceipts { get; set; } = new List<Transaction>();

    public void ReceiveItem(int itemId, int receivedQuantity)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            item.ReceivedQuantity = receivedQuantity;
        }
    }

    public void MarkAsReceived()
    {
        IsReceived = true;
    }
}
