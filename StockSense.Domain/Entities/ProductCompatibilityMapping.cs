namespace StockSense.Domain.Entities;

public class ProductCompatibilityMapping
{
    public int MappingId { get; set; }
    public int CompatibilityId { get; set; }
    public int ProductId { get; set; }
    public string PartFunction { get; set; } = string.Empty;
    public bool IsOEM { get; set; }
    public string? Notes { get; set; }

    public MotorCompatibility MotorCompatibility { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
