namespace StockSense.Domain.Entities;

/// <summary>
/// A stable analytics identity. It is intentionally independent from the operational
/// <see cref="Product"/> catalog because historical source identifiers can conflict with
/// live product identifiers.
/// </summary>
public sealed class ReportingProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<HistoricalProductMapping> HistoricalMappings { get; set; } =
        new List<HistoricalProductMapping>();

    public LiveProductMapping? LiveProductMapping { get; set; }

    public ICollection<HistoricalMonthlyProductSale> HistoricalMonthlySales { get; set; } =
        new List<HistoricalMonthlyProductSale>();
}
