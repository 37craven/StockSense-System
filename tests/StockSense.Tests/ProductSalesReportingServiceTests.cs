using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;
using StockSense.Web.Services;

namespace StockSense.Tests;

public sealed class ProductSalesReportingServiceTests
{
    [Fact]
    public async Task LinkLiveProductAsync_RejectsCutoverBeforeMonthAfterLatestHistory()
    {
        await using var context = CreateContext();
        var (reportingProductId, liveProductId) = await AddProductsWithHistoryAsync(context);
        var service = new ProductSalesReportingService(context);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.LinkLiveProductAsync(reportingProductId, liveProductId, new DateTime(2025, 6, 1)));

        Assert.Contains("July 2025", exception.Message, StringComparison.Ordinal);
        Assert.Empty(context.LiveProductMappings);
    }

    [Fact]
    public async Task LinkLiveProductAsync_AcceptsMonthImmediatelyAfterLatestHistory()
    {
        await using var context = CreateContext();
        var (reportingProductId, liveProductId) = await AddProductsWithHistoryAsync(context);
        var service = new ProductSalesReportingService(context);

        var mapping = await service.LinkLiveProductAsync(
            reportingProductId,
            liveProductId,
            new DateTime(2025, 7, 1));

        Assert.Equal(new DateTime(2025, 7, 1), mapping.UseTransactionsFrom);
        Assert.Single(context.LiveProductMappings);
    }

    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static async Task<(int ReportingProductId, int LiveProductId)> AddProductsWithHistoryAsync(
        ApplicationDbContext context)
    {
        var reportingProduct = new ReportingProduct { Name = "Road Helmet", Brand = "Acme", Category = "Safety" };
        var liveProduct = new Product { Name = "Road Helmet", Brand = "Acme", Category = "Safety", Price = 100 };
        context.AddRange(reportingProduct, liveProduct);
        await context.SaveChangesAsync();
        context.HistoricalMonthlyProductSales.Add(new HistoricalMonthlyProductSale
        {
            ReportingProductId = reportingProduct.Id,
            Year = 2025,
            Month = 6,
            QuantitySold = 12
        });
        await context.SaveChangesAsync();
        return (reportingProduct.Id, liveProduct.Id);
    }
}
