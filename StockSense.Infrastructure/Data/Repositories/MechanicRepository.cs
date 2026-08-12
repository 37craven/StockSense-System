using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Repositories;

public class MechanicRepository
{
    private readonly ApplicationDbContext _context;

    public MechanicRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Mechanic>> GetAllAsync()
    {
        return await _context.Mechanics.ToListAsync();
    }

    public async Task<List<Mechanic>> GetActiveAsync()
    {
        return await _context.Mechanics.Where(m => m.IsActive).ToListAsync();
    }

    public async Task<Mechanic?> GetByIdAsync(int id)
    {
        return await _context.Mechanics.FindAsync(id);
    }

    public async Task AddAsync(Mechanic mechanic)
    {
        _context.Mechanics.Add(mechanic);
        await Task.CompletedTask;
    }

    public Task<bool> NameExistsAsync(string name, int? excludingId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToUpper();
        return _context.Mechanics.AnyAsync(
            mechanic => (!excludingId.HasValue || mechanic.Id != excludingId.Value) &&
                        mechanic.Name.Trim().ToUpper() == normalizedName,
            cancellationToken);
    }

    public async Task UpdateAsync(Mechanic mechanic)
    {
        _context.Mechanics.Update(mechanic);
        await Task.CompletedTask;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var mechanic = await _context.Mechanics.FindAsync(id);
        if (mechanic == null) return false;
        _context.Mechanics.Remove(mechanic);
        return true;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
