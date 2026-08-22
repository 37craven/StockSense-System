using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data.Repositories;

namespace StockSense.Web.Helpers;

// ponytail: concrete helper, no interface — one consumer (POS razor)
public class TransactionHelper
{
    private static readonly HashSet<string> SupportedPaymentMethods =
        new(StringComparer.OrdinalIgnoreCase) { "Cash", "Online" };

    private readonly TransactionRepository _repo;
    private readonly ISafetyStockCalculationService _calculationService;
    private readonly ILogger<TransactionHelper> _logger;

    public TransactionHelper(
        TransactionRepository repo,
        ISafetyStockCalculationService calculationService,
        ILogger<TransactionHelper> logger)
    {
        _repo = repo;
        _calculationService = calculationService;
        _logger = logger;
    }

    public async Task<ReceiptDto> ProcessSaleAsync(
        CheckoutRequestDto request,
        string? userId,
        string locationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Lines.Count == 0)
            throw new InvalidOperationException("The checkout must contain at least one product.");
        if (request.Lines.Any(line => line.ProductId <= 0 || line.Quantity <= 0 || line.DiscountAmount < 0))
            throw new InvalidOperationException("Checkout lines contain an invalid product, quantity, or discount.");
        if (request.Lines.Any(line => line.LostSalesQuantity < 0))
            throw new InvalidOperationException("Lost sales quantity cannot be negative.");
        if (request.Lines.Any(line => line.RequestedQuantity.HasValue && line.RequestedQuantity.Value < line.Quantity))
            throw new InvalidOperationException("Requested quantity cannot be less than sold quantity.");
        if (request.Lines.Select(line => line.ProductId).Distinct().Count() != request.Lines.Count)
            throw new InvalidOperationException("A product can only appear once in a checkout.");

        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            throw new InvalidOperationException("Select a payment method.");

        var paymentMethod = request.PaymentMethod.Trim();
        if (!SupportedPaymentMethods.Contains(paymentMethod))
            throw new InvalidOperationException("Select a supported payment method.");

        paymentMethod = SupportedPaymentMethods.Single(method =>
            string.Equals(method, paymentMethod, StringComparison.OrdinalIgnoreCase));
        locationId = string.IsNullOrWhiteSpace(locationId) ? "MAIN" : locationId.Trim();
        if (locationId.Length > 50)
            throw new InvalidOperationException("The POS location identifier is too long.");

        // Generated outside the retry delegate so a transient retry reuses the same
        // public transaction number instead of creating a second logical sale.
        var transactionDate = DateTime.Now;
        var invoiceNumber = $"TXN-{transactionDate:yyMMdd-HHss}-{InvoiceHelper.ShortCode()}";

        ReceiptDto receipt;
        try
        {
            receipt = await _repo.ExecuteInTransactionAsync(async operationCancellationToken =>
            {
                var productIds = request.Lines.Select(line => line.ProductId).ToArray();
                var products = await _repo.GetProductsByIdsAsync(productIds, operationCancellationToken);
                if (products.Count != productIds.Length)
                    throw new InvalidOperationException("One or more products no longer exist.");

                var productsById = products.ToDictionary(product => product.Id);
                var preparedLines = request.Lines.Select(line =>
                {
                    var product = productsById[line.ProductId];
                    var grossAmount = product.Price * line.Quantity;

                    if (!product.IsActive)
                        throw new InvalidOperationException(
                            $"{product.Name} is inactive and cannot be sold.");
                    if (product.CurrentStock < line.Quantity)
                        throw new InvalidOperationException(
                            $"Insufficient stock for {product.Name}. Available: {product.CurrentStock}, requested: {line.Quantity}.");
                    if (line.DiscountAmount > grossAmount)
                        throw new InvalidOperationException($"Discount cannot exceed the line amount for {product.Name}.");

                    return new PreparedSaleLine(product, line.Quantity, line.DiscountAmount, grossAmount - line.DiscountAmount, line);
                }).ToArray();

                var sale = new Transaction
                {
                    InvoiceNumber = invoiceNumber,
                    TransactionDate = transactionDate,
                    TransactionType = TransactionTypes.Sale,
                    PaymentMethod = paymentMethod,
                    ReferenceNumber = NormalizeOptional(request.ReferenceNumber, 100, "Reference number"),
                    UserId = NormalizeOptional(userId, 450, "User identifier"),
                    LocationId = locationId,
                    Remarks = NormalizeOptional(request.Remarks, 500, "Remarks"),
                    DiscountAmount = preparedLines.Sum(line => line.DiscountAmount),
                    TotalAmount = preparedLines.Sum(line => line.LineTotal),
                    Items = new List<TransactionItem>()
                };

                foreach (var line in preparedLines)
                {
                    var stockBefore = line.Product.CurrentStock;
                    line.Product.DeductStock(line.Quantity);
                    await _repo.UpdateAsync(line.Product);

                    var req = line.Source.RequestedQuantity ?? line.Quantity;
                    var lost = line.Source.LostSalesQuantity;
                    // Auto-derive if caller sent only RequestedQuantity
                    if (lost == 0 && req > line.Quantity)
                        lost = req - line.Quantity;

                    sale.Items.Add(new TransactionItem
                    {
                        ProductId = line.Product.Id,
                        ProductName = line.Product.Name,
                        UnitPrice = line.Product.Price,
                        UnitCost = line.Product.UnitCost,
                        Quantity = line.Quantity,
                        DiscountAmount = line.DiscountAmount,
                        LineTotal = line.LineTotal,
                        StockBefore = stockBefore,
                        StockAfter = line.Product.CurrentStock,
                        RequestedQuantity = req,
                        LostSalesQuantity = lost,
                        StockoutOccurred = lost > 0
                    });
                }

                await _repo.AddAsync(sale, operationCancellationToken);
                await _repo.SaveChangesAsync(operationCancellationToken);

                return new ReceiptDto
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
                    Items = sale.Items.Select(i => new ReceiptItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        UnitPrice = i.UnitPrice,
                        Quantity = i.Quantity,
                        DiscountAmount = i.DiscountAmount,
                        LineTotal = i.LineTotal
                    }).ToList()
                };
            }, cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(exception, "Sale {InvoiceNumber} conflicted with another inventory update.", invoiceNumber);
            throw new InvalidOperationException(
                "Inventory changed while the sale was being processed. Review the latest stock and try again.",
                exception);
        }

        try
        {
            await _calculationService.RecalculateProductsAsync(
                request.Lines.Select(line => line.ProductId).Distinct(),
                locationId,
                cancellationToken);
        }
        catch (Exception exception)
        {
            // The sale is already committed. Calculation lag must never make the POS
            // report that a successful sale failed or invite the cashier to submit it again.
            _logger.LogWarning(
                exception,
                "Sale {InvoiceNumber} committed, but safety-stock recalculation did not complete.",
                receipt.InvoiceNumber);
        }

        return receipt;
    }

    private static string? NormalizeOptional(string? value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
            throw new InvalidOperationException($"{fieldName} cannot exceed {maximumLength} characters.");

        return normalized;
    }

    private sealed record PreparedSaleLine(
        Product Product,
        int Quantity,
        decimal DiscountAmount,
        decimal LineTotal,
        CheckoutLineDto Source);
}
