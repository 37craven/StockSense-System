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

    public Task<bool> ExistsAsync(
        string brand,
        string model,
        string baseCC,
        CancellationToken cancellationToken = default)
    {
        var normalizedBrand = brand.Trim().ToUpper();
        var normalizedModel = model.Trim().ToUpper();
        var normalizedBaseCC = baseCC.Trim().ToUpper();
        return _context.Motorcycles.AnyAsync(
            motorcycle => motorcycle.Brand.Trim().ToUpper() == normalizedBrand
                && motorcycle.Model.Trim().ToUpper() == normalizedModel
                && motorcycle.BaseCC.Trim().ToUpper() == normalizedBaseCC,
            cancellationToken);
    }

    public async Task<bool> HasActiveUsageAsync(int motorcycleId, CancellationToken cancellationToken = default)
    {
        // Active = PreBuiltPackage.IsActive OR BuildRequest/Appointment Pending/Confirmed
        var activePackage = await _context.PreBuiltPackages
            .AnyAsync(p => p.IsActive && p.CompatibleMotors.Any(m => m.MotorcycleId == motorcycleId), cancellationToken);
        if (activePackage) return true;

        var activeBuild = await _context.BuildRequests
            .AnyAsync(b => b.MotorcycleId == motorcycleId
                && (b.Status == "Pending" || b.Status == "Confirmed"), cancellationToken);
        if (activeBuild) return true;

        var activeAppointment = await _context.Appointments
            .AnyAsync(a => a.MotorcycleId == motorcycleId
                && (a.Status == "Pending" || a.Status == "Confirmed"), cancellationToken);
        return activeAppointment;
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
