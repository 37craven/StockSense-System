using System.Data;
using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Repositories;

public class TransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Transaction>> GetFilteredAsync(string? typeFilter = null, int take = 500)
    {
        var query = _context.Transactions
            .Include(t => t.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(typeFilter))
            query = query.Where(t => t.TransactionType == typeFilter);

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Transaction?> GetByIdWithItemsAsync(int id)
    {
        return await _context.Transactions
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<Product>> GetProductsByIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await Task.CompletedTask;
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
    }

    public Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return executionStrategy.ExecuteAsync(async () =>
        {
            // POS catalog reads share this scoped context. Clear them so checkout always
            // reloads authoritative stock/prices, and so a retry cannot deduct twice.
            _context.ChangeTracker.Clear();
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    public async Task VoidSaleAsync(int transactionId, string? reason = null, string? actorUserId = null)
    {
        var txn = await _context.Transactions
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == transactionId);
        if (txn == null || txn.IsVoided) return;

        var productIds = txn.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
        var lookup = products.ToDictionary(p => p.Id);

        var reversal = new Transaction
        {
            InvoiceNumber = $"RVT-{DateTime.Now:yyMMdd-HHss}-{InvoiceHelper.ShortCode()}",
            TransactionDate = DateTime.Now,
            TransactionType = TransactionTypes.StockCorrection,
            PaymentMethod = "N/A",
            LocationId = txn.LocationId,
            UserId = actorUserId,
            Remarks = string.IsNullOrWhiteSpace(reason)
                ? $"Stock restored from voided sale {txn.InvoiceNumber}"
                : $"Stock restored from voided sale {txn.InvoiceNumber}. Reason: {reason.Trim()}",
            TotalAmount = 0,
            IsVoided = false
        };

        foreach (var item in txn.Items)
        {
            if (lookup.TryGetValue(item.ProductId, out var product))
            {
                var stockBefore = product.CurrentStock;
                product.AddStock(item.Quantity);
                reversal.Items.Add(new TransactionItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    UnitCost = item.UnitCost,
                    Quantity = item.Quantity,
                    StockBefore = stockBefore,
                    StockAfter = product.CurrentStock,
                    LineTotal = 0
                });
            }
        }

        txn.IsVoided = true;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            var voidRemark = string.IsNullOrWhiteSpace(txn.Remarks)
                ? $"Voided. Reason: {reason.Trim()}"
                : $"{txn.Remarks} | Voided. Reason: {reason.Trim()}";
            txn.Remarks = voidRemark.Length <= 500 ? voidRemark : voidRemark[..500];
        }
        _context.Transactions.Add(reversal);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
