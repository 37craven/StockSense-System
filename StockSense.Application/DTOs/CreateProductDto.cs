namespace StockSense.Application.DTOs;

public class CreateProductDto
{
    public string Name { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public int InitialStock { get; set; }
    public int ReorderTarget { get; set; }
    public string ImageUrl { get; set; } = "https://placehold.co/300x200";
}
