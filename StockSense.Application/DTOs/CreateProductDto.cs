using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public class CreateProductDto
{
    public string Name { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Category { get; set; } = "";
    [Range(0.01, 9_999_999)]
    public decimal Price { get; set; }
    [Range(typeof(decimal), "0", "9999999")]
    public decimal UnitCost { get; set; } = 0m;
    public int InitialStock { get; set; }
    public int ReorderTarget { get; set; }
    public int? SupplierId { get; set; }
    public string ImageUrl { get; set; } = "";
    public string? Barcode { get; set; }
}
