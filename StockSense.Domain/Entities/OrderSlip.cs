namespace StockSense.Domain.Entities;

public class OrderSlip
{
    public int Id { get; set; }
    public string SlipNumber { get; set; } = string.Empty;
    public DateTime DateGenerated { get; set; } = DateTime.Now;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public List<OrderSlipItem> Items { get; set; } = new();
    public bool IsReceived { get; set; }

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