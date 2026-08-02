using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Repositories;

public class MotorcycleRepository
{
    private readonly ApplicationDbContext _context;

    public MotorcycleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Motorcycle>> GetAllAsync()
    {
        return await _context.Motorcycles
            .OrderBy(m => m.Brand)
            .ThenBy(m => m.Model)
            .ToListAsync();
    }

    public async Task<List<Motorcycle>> GetSelectableAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Motorcycles
            .AsNoTracking()
            .OrderBy(m => m.Brand)
            .ThenBy(m => m.Model)
            .ThenBy(m => m.BaseCC)
            .ToListAsync(cancellationToken);
    }

    public Task<Motorcycle?> GetSelectableByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Motorcycles
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task AddAsync(Motorcycle motorcycle)
    {
        _context.Motorcycles.Add(motorcycle);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var motorcycle = await _context.Motorcycles.FindAsync(id);
        if (motorcycle != null)
        {
            _context.Motorcycles.Remove(motorcycle);
            await _context.SaveChangesAsync();
        }
    }
}
