namespace StockSense.Domain.Entities;

public class SalesHistory
{
    public int Id { get; set; } // EF Core will automatically see this as the Primary Key
    public string Date { get; set; } = string.Empty;
    public string ProductID { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public float QtySold { get; set; }
    public float UnitPrice { get; set; }
    public float TotalSales { get; set; }
    public float MonthNum { get; set; }
    public float Year { get; set; }
}