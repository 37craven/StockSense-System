namespace StockSense.Domain.Entities;

public enum SalesImportStatus
{
    Pending,
    Completed,
    Failed
}

/// <summary>
/// Audit record for a historical sales import. Source plus SHA-256 hash provides
/// idempotency for repeated startup imports of the same file.
/// </summary>
public sealed class SalesImportBatch
{
    public int Id { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentSha256 { get; set; } = string.Empty;
    public SalesImportStatus Status { get; set; } = SalesImportStatus.Pending;
    public int RowsRead { get; set; }
    public int RowsInserted { get; set; }
    public int RowsUpdated { get; set; }
    public int ReportingProductsCreated { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }

    public ICollection<HistoricalMonthlyProductSale> HistoricalMonthlySales { get; set; } =
        new List<HistoricalMonthlyProductSale>();
}
