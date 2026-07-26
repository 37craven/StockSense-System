using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

namespace StockSense.Infrastructure.Services;

public sealed class SafetyStockCalculationService : ISafetyStockCalculationService
{
    private const int SaveBatchSize = 250;
    private const string ConcurrencyMessage =
        "The record was changed by another user. Reload the latest data and try again.";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<SafetyStockCalculationService> _logger;

    public SafetyStockCalculationService(
        ApplicationDbContext context,
        ILogger<SafetyStockCalculationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SafetyStockCalculationResult> RecalculateProductAsync(
        int productId,
        string locationId,
        CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productId));

        var results = await RecalculateCoreAsync([productId], false, locationId, cancellationToken);
        return results.Single();
    }

    public Task<IReadOnlyList<SafetyStockCalculationResult>> RecalculateProductsAsync(
        IEnumerable<int> productIds,
        string locationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productIds);
        var ids = productIds.Distinct().ToArray();
        if (ids.Length == 0)
            return Task.FromResult<IReadOnlyList<SafetyStockCalculationResult>>([]);
        if (ids.Any(id => id <= 0))
            throw new ArgumentOutOfRangeException(nameof(productIds));

        return RecalculateCoreAsync(ids, false, locationId, cancellationToken);
    }

    public Task<IReadOnlyList<SafetyStockCalculationResult>> RecalculateAllAsync(
        string locationId,
        CancellationToken cancellationToken = default) =>
        RecalculateCoreAsync([], true, locationId, cancellationToken);

    private async Task<IReadOnlyList<SafetyStockCalculationResult>> RecalculateCoreAsync(
        IReadOnlyCollection<int> requestedProductIds,
        bool includeAllProducts,
        string locationId,
        CancellationToken cancellationToken)
    {
        locationId = NormalizeLocation(locationId);
        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();
                await using var databaseTransaction = await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

                var productQuery = _context.Products.AsQueryable();
                if (!includeAllProducts)
                    productQuery = productQuery.Where(product => requestedProductIds.Contains(product.Id));

                var products = await productQuery.OrderBy(product => product.Id).ToListAsync(cancellationToken);
                if (!includeAllProducts && products.Count != requestedProductIds.Count)
                    throw new InvalidOperationException("One or more products no longer exist.");
                if (products.Count == 0)
                {
                    await databaseTransaction.CommitAsync(cancellationToken);
                    return (IReadOnlyList<SafetyStockCalculationResult>)[];
                }

                var calculationTime = DateTime.Now;
                var calculationDate = calculationTime.Date;
                var productIds = products.Select(product => product.Id).ToArray();

                var settings = await _context.ProductInventorySettings
                    .Where(setting => productIds.Contains(setting.ProductId) && setting.LocationId == locationId)
                    .ToListAsync(cancellationToken);

                await AddMissingSettingsAsync(
                    products,
                    settings,
                    locationId,
                    calculationDate,
                    cancellationToken);

                foreach (var setting in settings)
                {
                    SafetyStockMath.ValidateSetting(setting);
                    if (setting.InventoryTrackingStartDate.Date > calculationDate)
                        throw new InvalidOperationException(
                            $"Inventory tracking start date for product {setting.ProductId} cannot be in the future.");
                }

                var earliestTrackingDate = settings.Min(setting => setting.InventoryTrackingStartDate.Date);
                var demandRows = await (
                        from transaction in _context.Transactions.AsNoTracking()
                        join item in _context.TransactionItems.AsNoTracking()
                            on transaction.Id equals item.TransactionId
                        where productIds.Contains(item.ProductId)
                              && transaction.LocationId == locationId
                              && transaction.TransactionType.ToUpper() == TransactionTypes.Sale.ToUpper()
                              && transaction.TransactionDate >= earliestTrackingDate
                              && transaction.TransactionDate < calculationDate.AddDays(1)
                        group item by new { item.ProductId, Date = transaction.TransactionDate.Date }
                        into daily
                        select new
                        {
                            daily.Key.ProductId,
                            daily.Key.Date,
                            Demand = daily.Sum(item => item.Quantity + item.LostSalesQuantity)
                        })
                    .ToListAsync(cancellationToken);

                if (demandRows.Any(row => row.Demand < 0))
                    throw new InvalidOperationException("Historical demand cannot be negative.");

                var demandByProduct = demandRows
                    .GroupBy(row => row.ProductId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.ToDictionary(row => row.Date, row => row.Demand));

                var supplierIds = products
                    .Where(product => product.SupplierId.HasValue)
                    .Select(product => product.SupplierId!.Value)
                    .Distinct()
                    .ToArray();
                List<CompletedOrderDates> completedOrders = supplierIds.Length == 0
                    ? []
                    : await _context.OrderSlips.AsNoTracking()
                        .Where(slip => supplierIds.Contains(slip.SupplierId)
                                       && slip.Status == OrderSlipStatuses.Completed
                                       && slip.OrderedAt.HasValue
                                       && slip.CompletedAt.HasValue)
                        .Select(slip => new CompletedOrderDates(
                            slip.SupplierId,
                            slip.OrderedAt!.Value,
                            slip.CompletedAt!.Value))
                        .ToListAsync(cancellationToken);

                var leadTimesBySupplier = completedOrders
                    .Select(order => new
                    {
                        order.SupplierId,
                        Days = (decimal)(order.CompletedAt.Date - order.OrderedAt.Date).Days
                    })
                    .Where(order => order.Days > 0)
                    .GroupBy(order => order.SupplierId)
                    .ToDictionary(
                        group => group.Key,
                        group => (IReadOnlyList<decimal>)group.Select(order => order.Days).ToArray());

                var metrics = await _context.ProductInventoryMetrics
                    .Where(metric => productIds.Contains(metric.ProductId) && metric.LocationId == locationId)
                    .ToDictionaryAsync(metric => metric.ProductId, cancellationToken);
                var settingsByProduct = settings.ToDictionary(setting => setting.ProductId);
                var results = new List<SafetyStockCalculationResult>(products.Count);

                foreach (var product in products)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var setting = settingsByProduct[product.Id];
                    demandByProduct.TryGetValue(product.Id, out var observedByDate);
                    var dailyDemand = BuildCompleteDemandSeries(
                        setting.InventoryTrackingStartDate.Date,
                        calculationDate,
                        observedByDate);
                    var leadTimes = product.SupplierId.HasValue
                                    && leadTimesBySupplier.TryGetValue(product.SupplierId.Value, out var observedLeadTimes)
                        ? observedLeadTimes
                        : [];
                    var policy = SafetyStockMath.Calculate(setting, dailyDemand, leadTimes);
                    var totalObservedDemand = checked(dailyDemand.Sum(value => decimal.ToInt32(value)));

                    if (!metrics.TryGetValue(product.Id, out var metric))
                    {
                        metric = new ProductInventoryMetric
                        {
                            ProductId = product.Id,
                            LocationId = locationId
                        };
                        metrics.Add(product.Id, metric);
                        await _context.ProductInventoryMetrics.AddAsync(metric, cancellationToken);
                    }

                    ApplyMetric(
                        metric,
                        policy,
                        setting.ServiceLevel,
                        dailyDemand.Count,
                        totalObservedDemand,
                        calculationTime);
                    product.ReorderTarget = policy.ReorderPoint;
                    results.Add(ToResult(product, locationId, metric, policy, setting.ServiceLevel, calculationTime));

                    if (results.Count % SaveBatchSize == 0)
                        await _context.SaveChangesAsync(cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);
                await databaseTransaction.CommitAsync(cancellationToken);
                return results;
            });
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(exception, "Inventory recalculation encountered an optimistic concurrency conflict.");
            throw new InvalidOperationException(ConcurrencyMessage, exception);
        }
    }

    private async Task AddMissingSettingsAsync(
        IReadOnlyCollection<Product> products,
        ICollection<ProductInventorySetting> settings,
        string locationId,
        DateTime calculationDate,
        CancellationToken cancellationToken)
    {
        var configuredIds = settings.Select(setting => setting.ProductId).ToHashSet();
        var missingIds = products.Select(product => product.Id).Where(id => !configuredIds.Contains(id)).ToArray();
        if (missingIds.Length == 0)
            return;

        var firstSaleDates = await (
                from transaction in _context.Transactions.AsNoTracking()
                join item in _context.TransactionItems.AsNoTracking()
                    on transaction.Id equals item.TransactionId
                where missingIds.Contains(item.ProductId)
                      && transaction.LocationId == locationId
                      && transaction.TransactionType.ToUpper() == TransactionTypes.Sale.ToUpper()
                group transaction by item.ProductId
                into productTransactions
                select new
                {
                    ProductId = productTransactions.Key,
                    FirstDate = productTransactions.Min(transaction => transaction.TransactionDate)
                })
            .ToDictionaryAsync(row => row.ProductId, row => row.FirstDate, cancellationToken);

        foreach (var productId in missingIds)
        {
            var setting = new ProductInventorySetting
            {
                ProductId = productId,
                LocationId = locationId,
                InventoryTrackingStartDate = firstSaleDates.TryGetValue(productId, out var firstSale)
                    ? firstSale.Date
                    : calculationDate
            };
            settings.Add(setting);
            await _context.ProductInventorySettings.AddAsync(setting, cancellationToken);
        }
    }

    private static List<decimal> BuildCompleteDemandSeries(
        DateTime startDate,
        DateTime calculationDate,
        IReadOnlyDictionary<DateTime, int>? observedByDate)
    {
        var numberOfDays = checked((calculationDate - startDate).Days + 1);
        if (numberOfDays <= 0)
            throw new InvalidOperationException("Inventory tracking produced an empty date range.");

        var result = new List<decimal>(numberOfDays);
        for (var offset = 0; offset < numberOfDays; offset++)
        {
            var date = startDate.AddDays(offset);
            result.Add(observedByDate != null && observedByDate.TryGetValue(date, out var demand)
                ? demand
                : 0m);
        }
        return result;
    }

    private static void ApplyMetric(
        ProductInventoryMetric metric,
        SafetyStockPolicyResult policy,
        decimal serviceLevel,
        int usableDataDays,
        int totalObservedDemand,
        DateTime calculationTime)
    {
        metric.AverageDailyDemand = decimal.Round(policy.AppliedAverageDailyDemand, 4);
        metric.DemandStandardDeviation = decimal.Round(policy.DemandStandardDeviation, 4);
        metric.AverageLeadTimeDays = decimal.Round(policy.AverageLeadTimeDays, 4);
        metric.LeadTimeStandardDeviation = decimal.Round(policy.LeadTimeStandardDeviation, 4);
        metric.SafetyStock = policy.SafetyStock;
        metric.TargetStock = policy.TargetStock;
        metric.UsableDataDays = usableDataDays;
        metric.TotalObservedDemand = totalObservedDemand;
        metric.CalculationStage = policy.Stage;
        metric.ConfidenceLevel = policy.Confidence;
        metric.CalculationReason = string.Concat(
            policy.Explanation,
            $" Service level {serviceLevel:0.0000} (Z={policy.ZScore:0.0000}); ",
            $"demand mean/stddev {policy.AppliedAverageDailyDemand:0.####}/{policy.DemandStandardDeviation:0.####}; ",
            $"lead-time mean/stddev {policy.AverageLeadTimeDays:0.####}/{policy.LeadTimeStandardDeviation:0.####}; ",
            $"safety/reorder/target {policy.SafetyStock}/{policy.ReorderPoint}/{policy.TargetStock}; ",
            $"manual override: {(policy.ManualOverrideUsed ? "yes" : "no")}.");
        metric.LastCalculatedAt = calculationTime;
        metric.CalculationVersion = InventoryDefaults.CalculationVersion;
    }

    private static SafetyStockCalculationResult ToResult(
        Product product,
        string locationId,
        ProductInventoryMetric metric,
        SafetyStockPolicyResult policy,
        decimal serviceLevel,
        DateTime calculationTime) =>
        new(
            product.Id,
            product.Name,
            locationId,
            policy.Stage,
            metric.AverageDailyDemand,
            metric.DemandStandardDeviation,
            metric.AverageLeadTimeDays,
            metric.LeadTimeStandardDeviation,
            metric.SafetyStock,
            product.ReorderTarget,
            metric.TargetStock,
            metric.UsableDataDays,
            metric.TotalObservedDemand,
            policy.Confidence,
            serviceLevel,
            policy.ZScore,
            policy.Explanation,
            policy.ManualOverrideUsed,
            calculationTime,
            metric.CalculationVersion);

    private static string NormalizeLocation(string locationId)
    {
        var normalized = string.IsNullOrWhiteSpace(locationId)
            ? InventoryDefaults.LocationId
            : locationId.Trim();
        if (normalized.Length > 50)
            throw new InvalidOperationException("Location identifier cannot exceed 50 characters.");
        return normalized;
    }

    private sealed record CompletedOrderDates(int SupplierId, DateTime OrderedAt, DateTime CompletedAt);
}
