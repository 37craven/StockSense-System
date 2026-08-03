namespace StockSense.Application.DTOs;

public class CartItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
