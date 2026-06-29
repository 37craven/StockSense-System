namespace StockSense.Application.DTOs;

public class MarkAsReceivedCommand
{
    public int SlipId { get; set; }
    public List<ReceivedItemCommand> Items { get; set; } = new();
}

public class ReceivedItemCommand
{
    public int ItemId { get; set; }
    public int ReceivedQuantity { get; set; }
}
