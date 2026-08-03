using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public class UpdateProductDto
{
    [Required]
    public int Id { get; set; }

    [Range(0.01, 9_999_999)]
    public decimal Price { get; set; }

    [Range(typeof(decimal), "0", "9999999")]
    public decimal? UnitCost { get; set; }

    [Range(0, 99_999)]
    public int? ReorderTarget { get; set; }

    [Range(0, 999_999)]
    public int? CurrentStock { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public string? Barcode { get; set; }

    public int? SupplierId { get; set; }
}

public sealed class UpdateProductInventoryDto
{
    [Required]
    public int Id { get; set; }

    [Range(typeof(decimal), "0.01", "9999999")]
    public decimal Price { get; set; }

    [Range(-999_999, 999_999)]
    public int StockAdjustment { get; set; }

    [Required, StringLength(500, MinimumLength = 1)]
    public string Reason { get; set; } = string.Empty;

    public byte[] ProductRowVersion { get; set; } = [];
}

public sealed record UpdateProductInventoryResultDto(
    int Id,
    decimal Price,
    int CurrentStock,
    byte[] ProductRowVersion,
    string? Warning = null);

public sealed record ProductImageUploadResultDto(
    int Id,
    string ImageUrl,
    byte[] ProductRowVersion);
