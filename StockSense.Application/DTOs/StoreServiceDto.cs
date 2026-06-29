namespace StockSense.Application.DTOs;

public class CreateStoreServiceDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = "General";
    public int EstimatedMinutes { get; set; }
}

public class StoreServiceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = "General";
    public int EstimatedMinutes { get; set; }
    public string Status { get; set; } = "Active";
    public List<ProductDto> RequiredProducts { get; set; } = new();
}
