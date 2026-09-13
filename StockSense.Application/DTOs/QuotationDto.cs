namespace StockSense.Application.DTOs;

public class QuotationDto
{
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime DateGenerated { get; set; } = DateTime.Now;
    public List<QuotationItemDto> Items { get; set; } = new();
    public decimal GrandTotal => Items.Sum(i => i.LineTotal);
}

public class QuotationItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}
