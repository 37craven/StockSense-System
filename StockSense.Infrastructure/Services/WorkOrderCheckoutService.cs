using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockSense.Application.DTOs;
using StockSense.Application.Exceptions;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

namespace StockSense.Infrastructure.Services;

public sealed class WorkOrderCheckoutService : IWorkOrderCheckoutService
{
    private static readonly HashSet<string> SupportedPaymentMethods =
        new(StringComparer.OrdinalIgnoreCase) { "Cash", "Online" };
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ApplicationDbContext _context;
    private readonly ISafetyStockCalculationService _calculationService;
    private readonly ILogger<WorkOrderCheckoutService> _logger;

    public WorkOrderCheckoutService(
        ApplicationDbContext context,
        ISafetyStockCalculationService calculationService,
        ILogger<WorkOrderCheckoutService> logger)
    {
        _context = context;
        _calculationService = calculationService;
        _logger = logger;
    }

    public Task<ReceiptDto> CompleteAppointmentAsync(
        int appointmentId,
        CompleteWorkOrderDto request,
        string? employeeUserId,
        string locationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CompleteAppointmentCoreAsync(
            appointmentId,
            NormalizePaymentMethod(request.PaymentMethod),
            NormalizeOptional(request.ReferenceNumber, 100, "Reference number"),
            NormalizeOptional(employeeUserId, 450, "User identifier"),
            NormalizeLocation(locationId),
            cancellationToken);
    }

    public Task<ReceiptDto> CompleteBuildAsync(
        int buildId,
        CompleteWorkOrderDto request,
        string? employeeUserId,
        string locationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CompleteBuildCoreAsync(
            buildId,
            NormalizePaymentMethod(request.PaymentMethod),
            NormalizeOptional(request.ReferenceNumber, 100, "Reference number"),
            NormalizeOptional(employeeUserId, 450, "User identifier"),
            NormalizeLocation(locationId),
            cancellationToken);
    }

    private async Task<ReceiptDto> CompleteAppointmentCoreAsync(
        int appointmentId,
        string paymentMethod,
        string? referenceNumber,
        string? employeeUserId,
        string locationId,
        CancellationToken cancellationToken)
    {
        var completedAt = DateTime.Now;
        var invoiceNumber = $"APT-{appointmentId}-{completedAt:yyMMddHHss}-{InvoiceHelper.ShortCode()}";
        var result = await ExecuteAsync(async () =>
        {
            var appointment = await _context.Appointments
                .Include(value => value.Transaction)
                .ThenInclude(value => value!.Items)
                .SingleOrDefaultAsync(value => value.Id == appointmentId, cancellationToken)
                ?? throw new KeyNotFoundException("Appointment not found.");

            if (appointment.Transaction is not null)
                return new CompletionResult(ToReceipt(appointment.Transaction), [], false);

            EnsureReadyForCheckout(appointment.Status, "appointment");
            var selections = ParseAppointmentProducts(appointment.SelectedProductsJson);
            var requested = await ResolveAppointmentProductsAsync(selections, cancellationToken);
            var products = await LoadProductsAsync(requested.Keys, cancellationToken);

            var quotedProductAmount = selections.Where(value => value.Selected).Sum(value => value.Price);
            var serviceAmount = Math.Max(0m, appointment.TotalAmount - quotedProductAmount);
            var sale = CreateSale(
                invoiceNumber,
                completedAt,
                paymentMethod,
                referenceNumber,
                employeeUserId,
                locationId,
                serviceAmount,
                $"Completed appointment #{appointment.Id} for {appointment.CustomerName}: {appointment.ServicesRequested}.");
            AddInventoryLines(sale, products, requested);
            sale.TotalAmount = serviceAmount + sale.Items.Sum(value => value.LineTotal);

            _context.Transactions.Add(sale);
            appointment.Status = WorkOrderStatuses.Completed;
            appointment.CompletedAt = completedAt;
            appointment.Transaction = sale;
            appointment.TotalAmount = sale.TotalAmount;
            await _context.SaveChangesAsync(cancellationToken);
            return new CompletionResult(ToReceipt(sale), requested.Keys.ToArray(), true);
        }, cancellationToken);

        await RefreshSafetyStockAsync(result, locationId, cancellationToken);
        return result.Receipt;
    }

    private async Task<ReceiptDto> CompleteBuildCoreAsync(
        int buildId,
        string paymentMethod,
        string? referenceNumber,
        string? employeeUserId,
        string locationId,
        CancellationToken cancellationToken)
    {
        var completedAt = DateTime.Now;
        var invoiceNumber = $"BLD-{buildId}-{completedAt:yyMMddHHss}-{InvoiceHelper.ShortCode()}";
        var result = await ExecuteAsync(async () =>
        {
            var build = await _context.BuildRequests
                .Include(value => value.Transaction)
                .ThenInclude(value => value!.Items)
                .SingleOrDefaultAsync(value => value.Id == buildId, cancellationToken)
                ?? throw new KeyNotFoundException("Build not found.");

            if (build.Transaction is not null)
                return new CompletionResult(ToReceipt(build.Transaction), [], false);

            EnsureReadyForCheckout(build.Status, "build");
            var requested = ParseBuildProducts(build.SelectedPartsJson);
            if (requested.Count == 0)
                throw new WorkOrderConflictException("The build has no valid inventory products to check out.");
            var products = await LoadProductsAsync(requested.Keys, cancellationToken);
            var sale = CreateSale(
                invoiceNumber,
                completedAt,
                paymentMethod,
                referenceNumber,
                employeeUserId,
                locationId,
                0m,
                $"Completed custom build #{build.Id}: {build.BuildName} for {build.CustomerName}.");
            AddInventoryLines(sale, products, requested);
            sale.TotalAmount = sale.Items.Sum(value => value.LineTotal);

            _context.Transactions.Add(sale);
            build.Status = WorkOrderStatuses.Completed;
            build.CompletedAt = completedAt;
            build.Transaction = sale;
            build.TotalPrice = sale.TotalAmount;
            await _context.SaveChangesAsync(cancellationToken);
            return new CompletionResult(ToReceipt(sale), requested.Keys.ToArray(), true);
        }, cancellationToken);

        await RefreshSafetyStockAsync(result, locationId, cancellationToken);
        return result.Receipt;
    }

    private async Task<CompletionResult> ExecuteAsync(
        Func<Task<CompletionResult>> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();
                await using var transaction = await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);
                var result = await operation();
                await transaction.CommitAsync(cancellationToken);
                return result;
            });
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new WorkOrderConflictException(
                "Inventory changed during checkout. Reload the work order and try again.",
                exception);
        }
    }

    private async Task<Dictionary<int, int>> ResolveAppointmentProductsAsync(
        IReadOnlyList<AppointmentProductSelection> selections,
        CancellationToken cancellationToken)
    {
        var selected = selections.Where(value => value.Selected).ToArray();
        var requested = selected.Where(value => value.Id > 0)
            .GroupBy(value => value.Id)
            .ToDictionary(group => group.Key, group => group.Count());
        var legacyNames = selected.Where(value => value.Id <= 0)
            .Select(value => value.Name.Trim())
            .Where(value => value.Length > 0)
            .ToArray();

        foreach (var nameGroup in legacyNames.GroupBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            var matches = await _context.Products
                .Where(product => product.Name == nameGroup.Key)
                .Select(product => product.Id)
                .ToListAsync(cancellationToken);
            if (matches.Count != 1)
                throw new WorkOrderConflictException(
                    $"Legacy appointment product '{nameGroup.Key}' could not be matched uniquely. Rebook or correct the product selection.");
            requested[matches[0]] = requested.GetValueOrDefault(matches[0]) + nameGroup.Count();
        }

        return requested;
    }

    private async Task<List<Product>> LoadProductsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken)
    {
        var ids = productIds.Distinct().ToArray();
        var products = await _context.Products.Where(value => ids.Contains(value.Id)).ToListAsync(cancellationToken);
        if (products.Count != ids.Length)
            throw new WorkOrderConflictException("One or more selected products no longer exist.");
        return products;
    }

    private static void AddInventoryLines(
        Transaction sale,
        IReadOnlyList<Product> products,
        IReadOnlyDictionary<int, int> requested)
    {
        foreach (var product in products)
        {
            var quantity = requested[product.Id];
            var stockBefore = product.CurrentStock;
            if (stockBefore < quantity)
                throw new WorkOrderConflictException(
                    $"Insufficient stock for {product.Name}. Available: {stockBefore}, requested: {quantity}.");
            product.DeductStock(quantity);
            sale.Items.Add(new TransactionItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                UnitCost = product.UnitCost,
                Quantity = quantity,
                LineTotal = product.Price * quantity,
                StockBefore = stockBefore,
                StockAfter = product.CurrentStock
            });
        }
    }

    private static Transaction CreateSale(
        string invoiceNumber,
        DateTime transactionDate,
        string paymentMethod,
        string? referenceNumber,
        string? employeeUserId,
        string locationId,
        decimal serviceAmount,
        string remarks) => new()
        {
            InvoiceNumber = invoiceNumber,
            TransactionDate = transactionDate,
            TransactionType = TransactionTypes.Sale,
            PaymentMethod = paymentMethod,
            ReferenceNumber = referenceNumber,
            UserId = employeeUserId,
            LocationId = locationId,
            Remarks = remarks.Length <= 500 ? remarks : remarks[..500],
            ServiceAmount = serviceAmount
        };

    private async Task RefreshSafetyStockAsync(
        CompletionResult result,
        string locationId,
        CancellationToken cancellationToken)
    {
        if (!result.Created || result.ProductIds.Length == 0) return;
        try
        {
            await _calculationService.RecalculateProductsAsync(result.ProductIds, locationId, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Work-order sale {InvoiceNumber} committed, but safety-stock recalculation failed.",
                result.Receipt.InvoiceNumber);
        }
    }

    private static IReadOnlyList<AppointmentProductSelection> ParseAppointmentProducts(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        var groups = JsonSerializer.Deserialize<List<AppointmentProductGroup>>(json, JsonOptions) ?? [];
        return groups.SelectMany(value => value.Products ?? []).ToArray();
    }

    private static Dictionary<int, int> ParseBuildProducts(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        var parts = JsonSerializer.Deserialize<List<BuildPart>>(json, JsonOptions) ?? [];
        return parts.Where(value => value.Id > 0)
            .GroupBy(value => value.Id)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    private static void EnsureReadyForCheckout(string status, string workOrderType)
    {
        if (!string.Equals(status, WorkOrderStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
            throw new WorkOrderConflictException($"Only a confirmed {workOrderType} can be completed.");
    }

    private static string NormalizePaymentMethod(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !SupportedPaymentMethods.Contains(value.Trim()))
            throw new WorkOrderConflictException("Select a supported payment method.");
        return SupportedPaymentMethods.Single(method =>
            string.Equals(method, value.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeLocation(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? InventoryDefaults.LocationId : value.Trim();
        if (normalized.Length > 50) throw new WorkOrderConflictException("The location identifier is too long.");
        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
            throw new WorkOrderConflictException($"{fieldName} is too long.");
        return normalized;
    }

    private static ReceiptDto ToReceipt(Transaction sale) => new()
    {
        Id = sale.Id,
        InvoiceNumber = sale.InvoiceNumber,
        TransactionDate = sale.TransactionDate,
        TransactionType = sale.TransactionType,
        PaymentMethod = sale.PaymentMethod,
        ReferenceNumber = sale.ReferenceNumber,
        Remarks = sale.Remarks,
        DiscountAmount = sale.DiscountAmount,
        ServiceAmount = sale.ServiceAmount,
        TotalAmount = sale.TotalAmount,
        Items = sale.Items.Select(value => new ReceiptItemDto
        {
            ProductId = value.ProductId,
            ProductName = value.ProductName,
            UnitPrice = value.UnitPrice,
            Quantity = value.Quantity,
            DiscountAmount = value.DiscountAmount,
            LineTotal = value.LineTotal
        }).ToList()
    };

    private sealed record CompletionResult(ReceiptDto Receipt, int[] ProductIds, bool Created);
    private sealed class AppointmentProductGroup
    {
        public List<AppointmentProductSelection>? Products { get; set; }
    }
    private sealed class AppointmentProductSelection
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool Selected { get; set; } = true;
    }
    private sealed class BuildPart
    {
        public int Id { get; set; }
    }
}
