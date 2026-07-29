using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

namespace StockSense.Web.Services;

/// <summary>
/// Builds a single monthly series from imported history and persisted sale transactions.
/// Historical observations are used before a mapping's cutover month; live transactions
/// are used from the cutover month onward.
/// </summary>
public sealed class ProductSalesReportingService : IProductSalesDatasetService
{
    private readonly ApplicationDbContext context;

    public ProductSalesReportingService(ApplicationDbContext context) => this.context = context;

    public async Task<ProductSalesDataset> GetDatasetAsync(CancellationToken cancellationToken = default)
    {
        var reportingProducts = await context.Set<ReportingProduct>()
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .Select(product => new ReportingProductRow(
                product.Id,
                product.Name,
                product.Brand,
                product.Category))
            .ToListAsync(cancellationToken);

        var historicalSales = await context.Set<HistoricalMonthlyProductSale>()
            .AsNoTracking()
            .Select(sale => new ReportingSaleRow(
                sale.ReportingProductId,
                sale.Year,
                sale.Month,
                sale.QuantitySold))
            .ToListAsync(cancellationToken);

        var mappings = await (
                from mapping in context.Set<LiveProductMapping>().AsNoTracking()
                join product in context.Products.AsNoTracking() on mapping.ProductId equals product.Id
                select new MappingRow(
                    mapping.ReportingProductId,
                    mapping.ProductId,
                    product.Name,
                    mapping.UseTransactionsFrom,
                    mapping.Reason))
            .ToListAsync(cancellationToken);

        var liveProducts = await context.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .Select(product => new LiveProductRow(
                product.Id,
                product.Name,
                product.Brand,
                product.Category))
            .ToListAsync(cancellationToken);

        // In the current domain model a persisted Transaction with type "Sale" is posted;
        // drafts are not stored in Transactions.
        var liveSales = await context.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.TransactionType == "Sale")
            .SelectMany(
                transaction => transaction.Items,
                (transaction, item) => new
                {
                    item.ProductId,
                    transaction.TransactionDate,
                    item.Quantity
                })
            .GroupBy(row => new
            {
                row.ProductId,
                Year = row.TransactionDate.Year,
                Month = row.TransactionDate.Month
            })
            .Select(group => new LiveSaleRow(
                group.Key.ProductId,
                group.Key.Year,
                group.Key.Month,
                group.Sum(row => row.Quantity)))
            .ToListAsync(cancellationToken);

        var historyByReportingProduct = historicalSales
            .GroupBy(sale => sale.ReportingProductId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var mappingByReportingProduct = mappings.ToDictionary(mapping => mapping.ReportingProductId);
        var mappedLiveProductIds = mappings.Select(mapping => mapping.ProductId).ToHashSet();
        var liveSalesByProduct = liveSales
            .GroupBy(sale => sale.ProductId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var products = new List<ProductSalesProduct>(reportingProducts.Count + liveProducts.Count);

        foreach (var reportingProduct in reportingProducts)
        {
            var history = historyByReportingProduct.GetValueOrDefault(reportingProduct.Id) ?? [];
            var suggestedCutover = GetSuggestedCutover(history);
            mappingByReportingProduct.TryGetValue(reportingProduct.Id, out var mapping);
            var combined = new Dictionary<(int Year, int Month), int>();

            foreach (var sale in history)
            {
                var monthStart = new DateTime(sale.Year, sale.Month, 1);
                if (mapping is null || monthStart < mapping.UseTransactionsFrom)
                {
                    combined[(sale.Year, sale.Month)] = sale.QuantitySold;
                }
            }

            if (mapping is not null && liveSalesByProduct.TryGetValue(mapping.ProductId, out var mappedLiveSales))
            {
                foreach (var sale in mappedLiveSales)
                {
                    var monthStart = new DateTime(sale.Year, sale.Month, 1);
                    if (monthStart >= mapping.UseTransactionsFrom)
                    {
                        combined[(sale.Year, sale.Month)] = sale.QuantitySold;
                    }
                }
            }

            var mappingDto = mapping is null
                ? null
                : new ProductSalesMapping(
                    reportingProduct.Id,
                    mapping.ProductId,
                    mapping.ProductName,
                    mapping.UseTransactionsFrom,
                    mapping.Reason);

            products.Add(new ProductSalesProduct(
                $"reporting:{reportingProduct.Id}",
                reportingProduct.Id,
                mapping?.ProductId,
                reportingProduct.Name,
                reportingProduct.Brand,
                reportingProduct.Category,
                mapping is null ? ProductSalesSourceStatus.HistoricalOnly : ProductSalesSourceStatus.Mapped,
                suggestedCutover,
                mappingDto,
                ToObservations(combined)));
        }

        foreach (var liveProduct in liveProducts.Where(product => !mappedLiveProductIds.Contains(product.Id)))
        {
            var liveProductSales = liveSalesByProduct.GetValueOrDefault(liveProduct.Id) ?? [];
            products.Add(new ProductSalesProduct(
                $"live:{liveProduct.Id}",
                null,
                liveProduct.Id,
                liveProduct.Name,
                liveProduct.Brand,
                liveProduct.Category,
                ProductSalesSourceStatus.LiveOnly,
                null,
                null,
                liveProductSales
                    .OrderBy(sale => sale.Year)
                    .ThenBy(sale => sale.Month)
                    .Select(sale => new ProductSalesObservation(sale.Year, sale.Month, sale.QuantitySold))
                    .ToArray()));
        }

        products = products
            .OrderBy(product => product.ProductName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(product => product.SelectionKey, StringComparer.Ordinal)
            .ToList();

        var years = products
            .SelectMany(product => product.Sales)
            .Select(sale => sale.Year)
            .Distinct()
            .Order()
            .ToArray();

        return new ProductSalesDataset(products, years);
    }

    public async Task<IReadOnlyList<LiveProductOption>> GetLiveProductsAsync(
        CancellationToken cancellationToken = default)
    {
        var mappingByLiveProduct = await context.Set<LiveProductMapping>()
            .AsNoTracking()
            .ToDictionaryAsync(mapping => mapping.ProductId, mapping => mapping.ReportingProductId, cancellationToken);

        var products = await context.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .Select(product => new LiveProductRow(
                product.Id,
                product.Name,
                product.Brand,
                product.Category))
            .ToListAsync(cancellationToken);

        return products
            .Select(product => new LiveProductOption(
                product.Id,
                product.Name,
                product.Brand,
                product.Category,
                mappingByLiveProduct.GetValueOrDefault(product.Id)))
            .ToArray();
    }

    public async Task<ProductSalesMapping> LinkLiveProductAsync(
        int reportingProductId,
        int liveProductId,
        DateTime? useTransactionsFrom = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var reportingProductExists = await context.Set<ReportingProduct>()
            .AnyAsync(product => product.Id == reportingProductId, cancellationToken);
        if (!reportingProductExists)
        {
            throw new InvalidOperationException("The reporting product does not exist.");
        }

        var liveProduct = await context.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Id == liveProductId, cancellationToken)
            ?? throw new InvalidOperationException("The live product does not exist.");

        var conflictingMapping = await context.Set<LiveProductMapping>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                mapping => mapping.ProductId == liveProductId &&
                           mapping.ReportingProductId != reportingProductId,
                cancellationToken);
        if (conflictingMapping is not null)
        {
            throw new InvalidOperationException("That live product is already linked to another reporting product.");
        }

        var minimumCutover = await GetSuggestedCutoverAsync(reportingProductId, cancellationToken);
        var cutover = useTransactionsFrom ?? minimumCutover;
        cutover = new DateTime(cutover.Year, cutover.Month, 1);
        if (cutover < minimumCutover)
        {
            throw new ArgumentException(
                $"Live transactions cannot start before {minimumCutover:MMMM yyyy}, the month after the latest historical observation.",
                nameof(useTransactionsFrom));
        }
        reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (reason?.Length > 500)
        {
            throw new ArgumentException("The mapping reason cannot exceed 500 characters.", nameof(reason));
        }

        var mapping = await context.Set<LiveProductMapping>()
            .SingleOrDefaultAsync(item => item.ReportingProductId == reportingProductId, cancellationToken);
        if (mapping is null)
        {
            mapping = new LiveProductMapping
            {
                ReportingProductId = reportingProductId,
                ProductId = liveProductId,
                UseTransactionsFrom = cutover,
                Reason = reason
            };
            context.Add(mapping);
        }
        else
        {
            mapping.ProductId = liveProductId;
            mapping.UseTransactionsFrom = cutover;
            mapping.Reason = reason;
            mapping.UpdatedAtUtc = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new ProductSalesMapping(reportingProductId, liveProductId, liveProduct.Name, cutover, reason);
    }

    public async Task UnlinkLiveProductAsync(
        int reportingProductId,
        CancellationToken cancellationToken = default)
    {
        var mapping = await context.Set<LiveProductMapping>()
            .SingleOrDefaultAsync(item => item.ReportingProductId == reportingProductId, cancellationToken);
        if (mapping is null)
        {
            return;
        }

        context.Remove(mapping);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<DateTime> GetSuggestedCutoverAsync(
        int reportingProductId,
        CancellationToken cancellationToken)
    {
        var latest = await context.Set<HistoricalMonthlyProductSale>()
            .AsNoTracking()
            .Where(sale => sale.ReportingProductId == reportingProductId)
            .OrderByDescending(sale => sale.Year)
            .ThenByDescending(sale => sale.Month)
            .Select(sale => new { sale.Year, sale.Month })
            .FirstOrDefaultAsync(cancellationToken);

        return latest is null
            ? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)
            : new DateTime(latest.Year, latest.Month, 1).AddMonths(1);
    }

    private static DateTime? GetSuggestedCutover(IReadOnlyCollection<ReportingSaleRow> history)
    {
        var latest = history.OrderByDescending(sale => sale.Year).ThenByDescending(sale => sale.Month).FirstOrDefault();
        return latest is null ? null : new DateTime(latest.Year, latest.Month, 1).AddMonths(1);
    }

    private static IReadOnlyList<ProductSalesObservation> ToObservations(
        IReadOnlyDictionary<(int Year, int Month), int> values) =>
        values.OrderBy(pair => pair.Key.Year)
            .ThenBy(pair => pair.Key.Month)
            .Select(pair => new ProductSalesObservation(pair.Key.Year, pair.Key.Month, pair.Value))
            .ToArray();

    private sealed record ReportingProductRow(int Id, string Name, string Brand, string Category);
    private sealed record ReportingSaleRow(int ReportingProductId, short Year, byte Month, int QuantitySold);
    private sealed record MappingRow(int ReportingProductId, int ProductId, string ProductName, DateTime UseTransactionsFrom, string? Reason);
    private sealed record LiveProductRow(int Id, string Name, string Brand, string Category);
    private sealed record LiveSaleRow(int ProductId, int Year, int Month, int QuantitySold);
}
