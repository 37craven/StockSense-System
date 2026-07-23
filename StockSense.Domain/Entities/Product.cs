using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StockSense.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Price { get; set; }
    public string ImageUrl { get; set; } = "https://placehold.co/300x200";
    [JsonInclude]
    public int CurrentStock { get; set; }
    public int ReorderTarget { get; set; }

    public void DeductStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive");
        if (CurrentStock < quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {CurrentStock}, requested: {quantity}");
        CurrentStock -= quantity;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive");
        CurrentStock += quantity;
    }
    [JsonIgnore] public virtual ICollection<StoreService> StoreServices { get; set; } = new List<StoreService>();
    // This links the product to the Supplier class we just made
    public int? SupplierId { get; set; }
    public virtual Supplier? Supplier { get; set; }
    [JsonIgnore] public List<PreBuiltPackage> PreBuiltPackages { get; set; } = new();
}