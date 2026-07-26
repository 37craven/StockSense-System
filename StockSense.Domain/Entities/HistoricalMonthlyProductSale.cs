namespace StockSense.Domain.Entities;

/// <summary>
/// One validated historical observation at reporting-product/month grain.
/// </summary>
public sealed class HistoricalMonthlyProductSale
{
    public int Id { get; set; }
    public int ReportingProductId { get; set; }
    public int SalesImportBatchId { get; set; }
    public short Year { get; set; }
    public byte Month { get; set; }
    public int QuantitySold { get; set; }

    public ReportingProduct ReportingProduct { get; set; } = null!;
    public SalesImportBatch SalesImportBatch { get; set; } = null!;
}
