namespace StockSense.Domain.Entities;

/// <summary>
/// Explicitly bridges one reporting identity to one live catalog product.
/// Transactions on or after <see cref="UseTransactionsFrom"/> replace historical
/// observations for the same reporting series.
/// </summary>
public sealed class LiveProductMapping
{
    public int ReportingProductId { get; set; }
    public int ProductId { get; set; }
    public DateTime UseTransactionsFrom { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ReportingProduct ReportingProduct { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
