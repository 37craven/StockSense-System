using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public class CreateProductDto
{
    [Required]
    [StringLength(25)]
    public string Name { get; set; } = "";
    [Required]
    [StringLength(25)]
    public string Brand { get; set; } = "";
    [Required]
    [StringLength(25)]
    public string Category { get; set; } = "";
    [Range(0.01, 9_999_999)]
    public decimal Price { get; set; }
    [Range(typeof(decimal), "0", "9999999")]
    public decimal UnitCost { get; set; } = 0m;
    [Range(0, int.MaxValue)]
    public int InitialStock { get; set; }
    [Range(0, int.MaxValue)]
    public int ReorderTarget { get; set; }
    [Required]
    public int? SupplierId { get; set; }
    public string ImageUrl { get; set; } = "";
    public string? Barcode { get; set; }
    public bool IsActive { get; set; } = true;
}
