using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Repositories;

public class ProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _context.Products
            .Include(p => p.Supplier)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<Product>> GetActiveAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<List<Product>> GetByIdsAsync(List<int> ids)
    {
        return await _context.Products
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Product product)
    {
        _context.Products.Remove(product);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode)
    {
        return await _context.Products
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Barcode == barcode);
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToUpper();
        return _context.Products.AnyAsync(
            product => product.Name.Trim().ToUpper() == normalizedName,
            cancellationToken);
    }

    public async Task<Product?> GetActiveByBarcodeAsync(string barcode)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.IsActive && p.Barcode == barcode);
    }
}
