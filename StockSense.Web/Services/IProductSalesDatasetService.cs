namespace StockSense.Web.Services;

public interface IProductSalesDatasetService
{
    Task<ProductSalesDataset> GetDatasetAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LiveProductOption>> GetLiveProductsAsync(
        CancellationToken cancellationToken = default);

    Task<ProductSalesMapping> LinkLiveProductAsync(
        int reportingProductId,
        int liveProductId,
        DateTime? useTransactionsFrom = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task UnlinkLiveProductAsync(
        int reportingProductId,
        CancellationToken cancellationToken = default);
}

public sealed record ProductSalesDataset(
    IReadOnlyList<ProductSalesProduct> Products,
    IReadOnlyList<int> Years);

public enum ProductSalesSourceStatus
{
    HistoricalOnly,
    LiveOnly,
    Mapped
}

public sealed record ProductSalesProduct(
    string SelectionKey,
    int? ReportingProductId,
    int? LiveProductId,
    string ProductName,
    string Brand,
    string Category,
    ProductSalesSourceStatus SourceStatus,
    DateTime? SuggestedCutoverMonth,
    ProductSalesMapping? Mapping,
    IReadOnlyList<ProductSalesObservation> Sales);

public sealed record ProductSalesMapping(
    int ReportingProductId,
    int LiveProductId,
    string LiveProductName,
    DateTime UseTransactionsFrom,
    string? Reason);

public sealed record LiveProductOption(
    int ProductId,
    string ProductName,
    string Brand,
    string Category,
    int? MappedReportingProductId);

public sealed record ProductSalesObservation(int Year, int Month, int QuantitySold);
