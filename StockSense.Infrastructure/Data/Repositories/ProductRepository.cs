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
        var normalized = (barcode ?? string.Empty).Trim();

        // UPC-A (12 digits) and EAN-13 (13 digits) encode the same
        // product: an EAN-13 of a UPC-A symbol is the 12 UPC digits
        // prefixed with a leading "0". ZXing reports the scanned
        // label as either form depending on the frame, so match both.
        if (normalized.Length == 12 && normalized.All(char.IsDigit))
        {
            return await FindActiveByBarcodeAsync(normalized)
                ?? await FindActiveByBarcodeAsync("0" + normalized);
        }

        if (normalized.Length == 13 && normalized[0] == '0' && normalized.All(char.IsDigit))
        {
            return await FindActiveByBarcodeAsync(normalized)
                ?? await FindActiveByBarcodeAsync(normalized[1..]);
        }

        return await FindActiveByBarcodeAsync(normalized);
    }

    private async Task<Product?> FindActiveByBarcodeAsync(string barcode)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.IsActive && p.Barcode != null && p.Barcode.Trim() == barcode);
    }
}
