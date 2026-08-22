namespace StockSense.Application.DTOs;

public record ProductDto(
    int Id,
    string Name,
    string Category = "",
    string Brand = "",
    decimal Price = 0,
    int CurrentStock = 0,
    int ReorderTarget = 0,
    int SupplierId = 0,
    string SupplierName = "",
    string ImageUrl = "",
    string? Barcode = null,
    decimal UnitCost = 0m,
    byte[]? RowVersion = null,
    bool IsActive = true

)
{
    public int AvailableStock => IsActive
        ? CurrentStock
        : 0;
}
