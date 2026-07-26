namespace StockSense.Domain.Entities;

/// <summary>
/// Maps an immutable source-system product key to a reporting identity.
/// This key is never interpreted as <see cref="Product.Id"/>.
/// </summary>
public sealed class HistoricalProductMapping
{
    public int Id { get; set; }
    public int ReportingProductId { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string ExternalProductKey { get; set; } = string.Empty;

    public ReportingProduct ReportingProduct { get; set; } = null!;
}
