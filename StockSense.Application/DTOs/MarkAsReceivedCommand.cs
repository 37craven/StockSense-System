namespace StockSense.Application.DTOs;

public class MarkAsReceivedCommand
{
    public int SlipId { get; set; }
    public string LocationId { get; set; } = "MAIN";
    public string? ReferenceNumber { get; set; }
    public string? Remarks { get; set; }
    public string? ReceivedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<ReceivedItemCommand> Items { get; set; } = new();
}

public class ReceivedItemCommand
{
    public int ItemId { get; set; }
    public int ReceivedQuantity { get; set; }
}
