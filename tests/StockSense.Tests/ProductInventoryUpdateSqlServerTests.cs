using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Data.Repositories;
using StockSense.Infrastructure.Services;
using StockSense.Web.Controllers;

namespace StockSense.Tests;

public sealed class ProductInventoryUpdateSqlServerTests
{
    private const string ConnectionVariable = "STOCKSENSE_TEST_SQL_CONNECTION";

    [Fact]
    public void NewProducts_are_active_by_default()
    {
        var product = new Product();
        var createCommand = new CreateProductDto();

        Assert.True(product.IsActive);
        Assert.True(createCommand.IsActive);
    }

    [SqlServerFact]
    public async Task ProductStatusUpdate_persists_and_returns_new_row_version()
    {
        await using var fixture = await Fixture.CreateAsync();
        var oldVersion = fixture.Context.Products.AsNoTracking()
            .Where(value => value.Id == fixture.ProductId)
            .Select(value => value.RowVersion)
            .Single();

        var action = await fixture.Controller.UpdateProductStatus(
            fixture.ProductId,
            new UpdateProductStatusDto { IsActive = false, ProductRowVersion = oldVersion },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        var result = Assert.IsType<UpdateProductStatusResultDto>(ok.Value);
        Assert.False(result.IsActive);
        Assert.NotEqual(oldVersion, result.ProductRowVersion);
        fixture.Context.ChangeTracker.Clear();
        Assert.False((await fixture.Context.Products.AsNoTracking()
            .SingleAsync(value => value.Id == fixture.ProductId)).IsActive);
    }

    [SqlServerFact]
    public async Task ProductStatusUpdate_with_stale_version_returns_conflict()
    {
        await using var fixture = await Fixture.CreateAsync();
        var staleVersion = fixture.Context.Products.AsNoTracking()
            .Where(value => value.Id == fixture.ProductId)
            .Select(value => value.RowVersion)
            .Single();
        await using (var competing = fixture.CreateContext())
        {
            var product = await competing.Products.SingleAsync(value => value.Id == fixture.ProductId);
            product.Price += 1m;
            await competing.SaveChangesAsync();
        }

        var action = await fixture.Controller.UpdateProductStatus(
            fixture.ProductId,
            new UpdateProductStatusDto { IsActive = false, ProductRowVersion = staleVersion },
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(action);
        fixture.Context.ChangeTracker.Clear();
        Assert.True((await fixture.Context.Products.AsNoTracking()
            .SingleAsync(value => value.Id == fixture.ProductId)).IsActive);
    }

    [SqlServerFact]
    public async Task StockCorrection_UsesSignedDeltaAndCreatesAuditTransaction()
    {
        await using var fixture = await Fixture.CreateAsync();

        var action = await fixture.Controller.UpdateProductInventory(
            fixture.ProductId,
            fixture.Command(price: 12m, stockAdjustment: 3, reason: "  Physical count correction  "),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        var result = Assert.IsType<UpdateProductInventoryResultDto>(ok.Value);
        Assert.Equal(8, result.CurrentStock);
        fixture.Context.ChangeTracker.Clear();
        var product = await fixture.Context.Products.AsNoTracking().SingleAsync(value => value.Id == fixture.ProductId);
        var transaction = await fixture.Context.Transactions.AsNoTracking().Include(value => value.Items)
            .SingleAsync(value => value.ReferenceNumber == $"PRODUCT-{fixture.ProductId}");
        Assert.Equal(12m, product.Price);
        Assert.Equal(4m, product.UnitCost);
        Assert.Equal(8, product.CurrentStock);
        Assert.Equal(TransactionTypes.StockCorrection, transaction.TransactionType);
        Assert.Equal("Price 10.00 -> 12.00; Reason: Physical count correction", transaction.Remarks);
        var item = Assert.Single(transaction.Items);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(5, item.StockBefore);
        Assert.Equal(8, item.StockAfter);
    }

    [SqlServerFact]
    public async Task PriceOnlyEdit_CreatesHeaderAuditWithoutStockItem()
    {
        await using var fixture = await Fixture.CreateAsync();

        var action = await fixture.Controller.UpdateProductInventory(
            fixture.ProductId,
            fixture.Command(price: 11m, stockAdjustment: 0, reason: "Annual price review"),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        fixture.Context.ChangeTracker.Clear();
        var transaction = await fixture.Context.Transactions.AsNoTracking().Include(value => value.Items)
            .SingleAsync(value => value.ReferenceNumber == $"PRODUCT-{fixture.ProductId}");
        Assert.Equal(TransactionTypes.Adjustment, transaction.TransactionType);
        Assert.Empty(transaction.Items);
        Assert.Equal("Price 10.00 -> 11.00; Reason: Annual price review", transaction.Remarks);
        Assert.Equal(5, (await fixture.Context.Products.AsNoTracking().SingleAsync(value => value.Id == fixture.ProductId)).CurrentStock);
    }

    [SqlServerFact]
    public async Task StaleProductVersion_ReturnsConflictAndCreatesNoAudit()
    {
        await using var fixture = await Fixture.CreateAsync();
        var command = fixture.Command(price: 12m, stockAdjustment: 1, reason: "Count correction");
        await using (var competing = fixture.CreateContext())
        {
            var product = await competing.Products.SingleAsync(value => value.Id == fixture.ProductId);
            product.Price = 10.50m;
            await competing.SaveChangesAsync();
        }

        var action = await fixture.Controller.UpdateProductInventory(
            fixture.ProductId, command, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(action);
        fixture.Context.ChangeTracker.Clear();
        Assert.False(await fixture.Context.Transactions.AsNoTracking()
            .AnyAsync(value => value.ReferenceNumber == $"PRODUCT-{fixture.ProductId}"));
        Assert.Equal(5, (await fixture.Context.Products.AsNoTracking().SingleAsync(value => value.Id == fixture.ProductId)).CurrentStock);
    }

    [SqlServerFact]
    public async Task ExcessiveDecimalPrecision_IsRejectedWithoutMutationOrAudit()
    {
        await using var fixture = await Fixture.CreateAsync();

        var action = await fixture.Controller.UpdateProductInventory(
            fixture.ProductId,
            fixture.Command(price: 10.001m, stockAdjustment: 1, reason: "Count correction"),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action);
        Assert.Contains("two decimal places", badRequest.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        fixture.Context.ChangeTracker.Clear();
        var product = await fixture.Context.Products.AsNoTracking().SingleAsync(value => value.Id == fixture.ProductId);
        Assert.Equal(10m, product.Price);
        Assert.Equal(5, product.CurrentStock);
        Assert.False(await fixture.Context.Transactions.AsNoTracking()
            .AnyAsync(value => value.ReferenceNumber == $"PRODUCT-{fixture.ProductId}"));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string _connectionString;
        private readonly string _token;

        private Fixture(string connectionString, string token, ApplicationDbContext context)
        {
            _connectionString = connectionString;
            _token = token;
            Context = context;
            var calculation = new SafetyStockCalculationService(context, NullLogger<SafetyStockCalculationService>.Instance);
            Controller = new ProductsController(
                new ProductRepository(context),
                new EmailSender(new ConfigurationBuilder().Build()),
                new BarcodeService(),
                context,
                calculation,
                NullLogger<ProductsController>.Instance)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity())
                    }
                }
            };
        }

        public ApplicationDbContext Context { get; }
        public ProductsController Controller { get; }
        public int ProductId { get; private set; }

        public static async Task<Fixture> CreateAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException($"Set {ConnectionVariable} to run SQL Server integration tests.");
            var fixture = new Fixture(connectionString, $"SS-PRODUCT-IT-{Guid.NewGuid():N}", CreateContext(connectionString));
            try
            {
                var product = new Product
                {
                    Name = fixture._token,
                    Category = "Integration Test",
                    Brand = "StockSense",
                    Price = 10m,
                    UnitCost = 4m,
                    CurrentStock = 5,
                    ReorderTarget = 2
                };
                fixture.Context.Products.Add(product);
                await fixture.Context.SaveChangesAsync();
                fixture.ProductId = product.Id;
                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync();
                throw;
            }
        }

        public ApplicationDbContext CreateContext() => CreateContext(_connectionString);

        public UpdateProductInventoryDto Command(decimal price, int stockAdjustment, string reason)
        {
            var version = Context.Products.AsNoTracking().Where(value => value.Id == ProductId)
                .Select(value => value.RowVersion).Single();
            return new UpdateProductInventoryDto
            {
                Id = ProductId,
                Price = price,
                StockAdjustment = stockAdjustment,
                Reason = reason,
                ProductRowVersion = version
            };
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var cleanup = CreateContext();
                await cleanup.Transactions.Where(value => value.ReferenceNumber == $"PRODUCT-{ProductId}").ExecuteDeleteAsync();
                await cleanup.ProductInventoryMetrics.Where(value => value.ProductId == ProductId).ExecuteDeleteAsync();
                await cleanup.ProductInventorySettings.Where(value => value.ProductId == ProductId).ExecuteDeleteAsync();
                await cleanup.Products.Where(value => value.Id == ProductId).ExecuteDeleteAsync();
            }
            finally
            {
                await Context.DisposeAsync();
            }
        }

        private static ApplicationDbContext CreateContext(string connectionString) =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null))
                .Options);
    }
}
