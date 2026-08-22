using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Repositories;

public class OrderSlipRepository
{
    private readonly ApplicationDbContext _context;

    public OrderSlipRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrderSlip>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OrderSlips
            .AsNoTracking()
            .Include(s => s.Supplier)
            .Include(s => s.Items)
            .OrderByDescending(s => s.DateGenerated)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderSlip?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.OrderSlips
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddSlipAsync(OrderSlip slip, CancellationToken cancellationToken = default)
    {
        await _context.OrderSlips.AddAsync(slip, cancellationToken);
    }

    public async Task UpdateSlipAsync(OrderSlip slip)
    {
        _context.OrderSlips.Update(slip);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var slip = await _context.OrderSlips.FindAsync([id], cancellationToken);
        if (slip != null) _context.OrderSlips.Remove(slip);
    }

    public async Task DeleteItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        var item = await _context.OrderSlipItems.FindAsync([itemId], cancellationToken);
        if (item != null) _context.OrderSlipItems.Remove(item);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OrderSlips.CountAsync(
            s => s.Status != OrderSlipStatuses.Completed
                 && s.Status != OrderSlipStatuses.ClosedShort
                 && s.Status != OrderSlipStatuses.Cancelled,
            cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
