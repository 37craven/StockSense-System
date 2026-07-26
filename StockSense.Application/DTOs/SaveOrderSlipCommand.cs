namespace StockSense.Application.DTOs;

public class SaveOrderSlipCommand
{
    public string SlipNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string LocationId { get; set; } = "MAIN";
    public string? CreatedByUserId { get; set; }
    public string? Remarks { get; set; }
    public List<SaveOrderSlipItemCommand> Items { get; set; } = new();
}

public class SaveOrderSlipItemCommand
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
