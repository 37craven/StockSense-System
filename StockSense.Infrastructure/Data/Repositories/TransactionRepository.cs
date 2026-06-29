using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Repositories;

public class TransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetProductByIdAsync(int productId)
    {
        return await _context.Products.FindAsync(productId);
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await Task.CompletedTask;
    }

    public async Task AddAsync(SalesHistory history)
    {
        _context.SalesHistory.Add(history);
        await Task.CompletedTask;
    }

    public async Task AddAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}