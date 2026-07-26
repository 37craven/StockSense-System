using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Services;

namespace StockSense.Tests;

public sealed class OrderSlipWorkflowSqlServerTests
{
    private const string ConnectionVariable = "STOCKSENSE_TEST_SQL_CONNECTION";

    [SqlServerFact]
    public async Task DuplicateDraftRequest_DoesNotCreateDuplicateOpenItem()
    {
        await using var fixture = await WorkflowFixture.CreateAsync();
        var command = fixture.CreateDraftCommand(10);

        var first = await fixture.Workflow.CreateDraftsAsync(command);
        var second = await fixture.Workflow.CreateDraftsAsync(command);

        Assert.True(first.IsSuccess);
        Assert.Single(first.Value!.OrderSlips);
        Assert.True(second.IsSuccess);
        Assert.Empty(second.Value!.OrderSlips);
        Assert.Contains(second.Value.Warnings, warning => warning.Code == "OPEN_ORDER_EXISTS");
        Assert.Equal(1, await fixture.Context.OrderSlipItems.CountAsync(item => item.ProductId == fixture.ProductId));
    }

    [SqlServerFact]
    public async Task PartialThenCompleteReceipt_UpdatesStockReceiptAuditAndStatusAtomically()
    {
        await using var fixture = await WorkflowFixture.CreateAsync();
        var ordered = await fixture.CreateOrderedSlipAsync(10);

        var partial = await fixture.Workflow.ReceiveAsync(fixture.CreateReceiptCommand(ordered, 4));

        Assert.True(partial.IsSuccess);
        Assert.Equal(OrderSlipStatuses.PartiallyReceived, partial.Value!.OrderSlipStatus);
        fixture.Context.ChangeTracker.Clear();
        var afterPartial = await fixture.LoadStateAsync(ordered.OrderSlipId);
        Assert.Equal(14, afterPartial.Stock);
        Assert.Equal(4, afterPartial.Received);
        Assert.Equal(OrderSlipStatuses.PartiallyReceived, afterPartial.Status);
        Assert.Equal(1, afterPartial.ReceiptCount);

        ordered = ordered with { RowVersion = afterPartial.RowVersion };
        var completed = await fixture.Workflow.ReceiveAsync(fixture.CreateReceiptCommand(ordered, 6));

        Assert.True(completed.IsSuccess);
        Assert.Equal(OrderSlipStatuses.Completed, completed.Value!.OrderSlipStatus);
        fixture.Context.ChangeTracker.Clear();
        var final = await fixture.LoadStateAsync(ordered.OrderSlipId);
        Assert.Equal(20, final.Stock);
        Assert.Equal(10, final.Received);
        Assert.Equal(OrderSlipStatuses.Completed, final.Status);
        Assert.Equal(2, final.ReceiptCount);
        Assert.NotNull(final.CompletedAt);
    }

    [SqlServerFact]
    public async Task OverReceipt_LeavesStockOrderAndTransactionsUnchanged()
    {
        await using var fixture = await WorkflowFixture.CreateAsync();
        var ordered = await fixture.CreateOrderedSlipAsync(10);

        var result = await fixture.Workflow.ReceiveAsync(fixture.CreateReceiptCommand(ordered, 11));

        Assert.False(result.IsSuccess);
        Assert.Equal("OVER_RECEIPT", result.ErrorCode);
        fixture.Context.ChangeTracker.Clear();
        var state = await fixture.LoadStateAsync(ordered.OrderSlipId);
        Assert.Equal(10, state.Stock);
        Assert.Equal(0, state.Received);
        Assert.Equal(OrderSlipStatuses.Ordered, state.Status);
        Assert.Equal(0, state.ReceiptCount);
    }

    [SqlServerFact]
    public async Task StaleOrderSlipRowVersion_ReturnsFriendlyConcurrencyConflict()
    {
        await using var fixture = await WorkflowFixture.CreateAsync();
        var draft = await fixture.CreateDraftAsync(10);

        await using (var competingContext = fixture.CreateContext())
        {
            var competingSlip = await competingContext.OrderSlips.SingleAsync(slip => slip.Id == draft.OrderSlipId);
            competingSlip.Remarks = "Concurrent integration-test update";
            await competingContext.SaveChangesAsync();
        }

        var result = await fixture.Workflow.ApproveAsync(new OrderSlipTransitionCommand
        {
            OrderSlipId = draft.OrderSlipId,
            TargetStatus = OrderSlipStatuses.Approved,
            RowVersion = draft.RowVersion
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsConcurrencyConflict);
        Assert.Equal("CONCURRENCY_CONFLICT", result.ErrorCode);
        Assert.Contains("Reload", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [SqlServerFact]
    public async Task RetryingExecutionStrategy_CanCommitWorkflowTransaction()
    {
        await using var fixture = await WorkflowFixture.CreateAsync();

        Assert.True(fixture.Context.Database.CreateExecutionStrategy().RetriesOnFailure);
        var result = await fixture.Workflow.CreateDraftsAsync(fixture.CreateDraftCommand(10));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.OrderSlips);
        Assert.Equal(1, await fixture.Context.OrderSlips.CountAsync(slip => slip.Items.Any(item => item.ProductId == fixture.ProductId)));
    }

    private sealed class WorkflowFixture : IAsyncDisposable
    {
        private readonly string _connectionString;
        private readonly string _token;

        private WorkflowFixture(string connectionString, string token, ApplicationDbContext context)
        {
            _connectionString = connectionString;
            _token = token;
            Context = context;
            var calculation = new SafetyStockCalculationService(
                context, NullLogger<SafetyStockCalculationService>.Instance);
            Workflow = new OrderSlipWorkflowService(
                context, calculation, NullLogger<OrderSlipWorkflowService>.Instance);
        }

        public ApplicationDbContext Context { get; }
        public OrderSlipWorkflowService Workflow { get; }
        public int SupplierId { get; private set; }
        public int ProductId { get; private set; }

        public static async Task<WorkflowFixture> CreateAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException($"Set {ConnectionVariable} to run SQL Server integration tests.");

            var token = $"SS-IT-{Guid.NewGuid():N}";
            var fixture = new WorkflowFixture(
                connectionString,
                token,
                CreateContext(connectionString));
            try
            {
                await fixture.SeedAsync();
                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync();
                throw;
            }
        }

        public ApplicationDbContext CreateContext() => CreateContext(_connectionString);

        public CreateOrderSlipDraftsCommand CreateDraftCommand(int quantity) => new()
        {
            LocationId = InventoryDefaults.LocationId,
            CreatedByUserId = null,
            Remarks = _token,
            SupplierGroups =
            [
                new CreateDraftOrderSlipGroupCommand
                {
                    SupplierId = SupplierId,
                    Items = [new CreateDraftOrderSlipItemCommand { ProductId = ProductId, OrderedQuantity = quantity }]
                }
            ]
        };

        public async Task<SlipToken> CreateDraftAsync(int quantity)
        {
            var result = await Workflow.CreateDraftsAsync(CreateDraftCommand(quantity));
            Assert.True(result.IsSuccess, result.ErrorMessage);
            var slip = Assert.Single(result.Value!.OrderSlips);
            return new(slip.Id, Assert.Single(slip.Items).Id, slip.RowVersion);
        }

        public async Task<SlipToken> CreateOrderedSlipAsync(int quantity)
        {
            var draft = await CreateDraftAsync(quantity);
            var approved = await Workflow.ApproveAsync(new OrderSlipTransitionCommand
            {
                OrderSlipId = draft.OrderSlipId,
                TargetStatus = OrderSlipStatuses.Approved,
                RowVersion = draft.RowVersion
            });
            Assert.True(approved.IsSuccess, approved.ErrorMessage);
            var ordered = await Workflow.MarkOrderedAsync(new OrderSlipTransitionCommand
            {
                OrderSlipId = draft.OrderSlipId,
                TargetStatus = OrderSlipStatuses.Ordered,
                RowVersion = approved.Value!.RowVersion
            });
            Assert.True(ordered.IsSuccess, ordered.ErrorMessage);
            return new(draft.OrderSlipId, draft.OrderSlipItemId, ordered.Value!.RowVersion);
        }

        public ReceiveOrderSlipCommand CreateReceiptCommand(SlipToken slip, int quantity) => new()
        {
            OrderSlipId = slip.OrderSlipId,
            LocationId = InventoryDefaults.LocationId,
            ReceivedAt = DateTime.Today,
            ReferenceNumber = $"REF-{_token}",
            RowVersion = slip.RowVersion,
            Items = [new ReceiveOrderSlipItemCommand { OrderSlipItemId = slip.OrderSlipItemId, QuantityReceived = quantity }]
        };

        public async Task<PersistedState> LoadStateAsync(int slipId)
        {
            var product = await Context.Products.AsNoTracking().SingleAsync(value => value.Id == ProductId);
            var slip = await Context.OrderSlips.AsNoTracking().Include(value => value.Items)
                .SingleAsync(value => value.Id == slipId);
            return new(
                product.CurrentStock,
                Assert.Single(slip.Items).ReceivedQuantity,
                slip.Status,
                slip.RowVersion,
                slip.CompletedAt,
                await Context.Transactions.AsNoTracking().CountAsync(transaction =>
                    transaction.OrderSlipId == slipId && transaction.TransactionType == TransactionTypes.PurchaseReceipt));
        }

        private async Task SeedAsync()
        {
            var supplier = new Supplier { Name = $"Supplier {_token}", Email = $"{_token}@example.test" };
            Context.Suppliers.Add(supplier);
            await Context.SaveChangesAsync();
            SupplierId = supplier.Id;

            var product = new Product
            {
                Name = $"Product {_token}", Category = "Integration Test", Brand = "StockSense",
                Price = 15m, UnitCost = 10m, CurrentStock = 10, ReorderTarget = 10,
                SupplierId = supplier.Id
            };
            Context.Products.Add(product);
            await Context.SaveChangesAsync();
            ProductId = product.Id;

            Context.ProductInventorySettings.Add(new ProductInventorySetting
            {
                ProductId = product.Id, LocationId = InventoryDefaults.LocationId,
                InitialEstimatedWeeklyDemand = 7m, DefaultLeadTimeDays = 7,
                ReviewPeriodDays = 7, BufferDays = 7, ServiceLevel = 0.9500m,
                MinimumOrderQuantity = 1, PackageSize = 1, IsAutomaticOrderEnabled = true,
                InventoryTrackingStartDate = DateTime.Today.AddDays(-1)
            });
            Context.ProductInventoryMetrics.Add(new ProductInventoryMetric
            {
                ProductId = product.Id, LocationId = InventoryDefaults.LocationId,
                AverageDailyDemand = 1m, AverageLeadTimeDays = 7m,
                SafetyStock = 5, TargetStock = 20, UsableDataDays = 2,
                CalculationStage = InventoryCalculationStages.ColdStart,
                ConfidenceLevel = InventoryConfidenceLevels.Low,
                CalculationReason = _token, LastCalculatedAt = DateTime.Now,
                CalculationVersion = InventoryDefaults.CalculationVersion
            });
            await Context.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var cleanup = CreateContext();
                var productIds = await cleanup.Products.Where(product => product.Name == $"Product {_token}")
                    .Select(product => product.Id).ToArrayAsync();
                var slipIds = productIds.Length == 0
                    ? []
                    : await cleanup.OrderSlipItems.Where(item => productIds.Contains(item.ProductId))
                        .Select(item => item.OrderSlipId).Distinct().ToArrayAsync();
                if (slipIds.Length > 0)
                {
                    await cleanup.Transactions.Where(transaction =>
                        transaction.OrderSlipId.HasValue && slipIds.Contains(transaction.OrderSlipId.Value))
                        .ExecuteDeleteAsync();
                    await cleanup.OrderSlips.Where(slip => slipIds.Contains(slip.Id)).ExecuteDeleteAsync();
                }
                if (productIds.Length > 0)
                {
                    await cleanup.ProductInventoryMetrics.Where(metric => productIds.Contains(metric.ProductId)).ExecuteDeleteAsync();
                    await cleanup.ProductInventorySettings.Where(setting => productIds.Contains(setting.ProductId)).ExecuteDeleteAsync();
                    await cleanup.Products.Where(product => productIds.Contains(product.Id)).ExecuteDeleteAsync();
                }
                await cleanup.Suppliers.Where(supplier => supplier.Name == $"Supplier {_token}").ExecuteDeleteAsync();
            }
            finally
            {
                await Context.DisposeAsync();
            }
        }

        private static ApplicationDbContext CreateContext(string connectionString)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null))
                .Options;
            return new ApplicationDbContext(options);
        }
    }

    private sealed record SlipToken(int OrderSlipId, int OrderSlipItemId, byte[] RowVersion);
    private sealed record PersistedState(
        int Stock,
        int Received,
        string Status,
        byte[] RowVersion,
        DateTime? CompletedAt,
        int ReceiptCount);
}

internal sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STOCKSENSE_TEST_SQL_CONNECTION")))
            Skip = "Set STOCKSENSE_TEST_SQL_CONNECTION to run SQL Server integration tests.";
    }
}
