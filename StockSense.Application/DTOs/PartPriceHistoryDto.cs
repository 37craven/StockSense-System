using StockSense.Domain.Entities;
namespace StockSense.Application.DTOs;

public sealed class PartPriceHistoryDto
{
    public int UpgradePartId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public string Trend { get; set; } = "Insufficient history";
    public List<PartPricePointDto> Points { get; set; } = new();
}

public sealed class PartPricePointDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal MedianUnitPrice { get; set; }
    public int Transactions { get; set; }
}
