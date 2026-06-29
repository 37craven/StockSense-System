using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Repositories;

public class PreBuiltRepository
{
    private readonly ApplicationDbContext _context;

    public PreBuiltRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PreBuiltPackage>> GetAllAsync()
    {
        return await _context.PreBuiltPackages
            .Include(p => p.IncludedProducts) 
            .ToListAsync();
    }

    public async Task<PreBuiltPackage?> GetByIdAsync(int id)
    {
        return await _context.PreBuiltPackages
            .Include(p => p.IncludedProducts)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddAsync(PreBuiltPackage package)
    {
        _context.PreBuiltPackages.Add(package);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PreBuiltPackage package)
    {
        _context.PreBuiltPackages.Update(package);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var package = await _context.PreBuiltPackages.FindAsync(id);
        if (package != null)
        {
            _context.PreBuiltPackages.Remove(package);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Product>> GetProductsByIdsAsync(List<int> productIds)
    {
        return await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();
    }
}