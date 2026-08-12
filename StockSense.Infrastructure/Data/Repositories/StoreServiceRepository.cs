using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Repositories;

public class StoreServiceRepository
{
    private readonly ApplicationDbContext _context;

    public StoreServiceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<StoreService>> GetAllWithProductsAsync()
    {
        return await _context.StoreServices
            .Include(s => s.RequiredProducts)
            .ToListAsync();
    }

    public async Task<StoreService?> GetByIdWithProductsAsync(int id)
    {
        return await _context.StoreServices
            .Include(s => s.RequiredProducts)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<StoreService>> GetByNamesAsync(List<string> names)
    {
        return await _context.StoreServices
            .Where(s => names.Contains(s.Name))
            .ToListAsync();
    }

    public async Task AddAsync(StoreService service)
    {
        _context.StoreServices.Add(service);
        await _context.SaveChangesAsync();
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToUpper();
        return _context.StoreServices.AnyAsync(
            service => service.Name.Trim().ToUpper() == normalizedName,
            cancellationToken);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
