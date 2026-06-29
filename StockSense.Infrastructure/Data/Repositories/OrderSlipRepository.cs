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

    public async Task<List<OrderSlip>> GetAllAsync()
    {
        return await _context.OrderSlips
            .Include(s => s.Supplier)
            .Include(s => s.Items)
            .OrderByDescending(s => s.DateGenerated)
            .ToListAsync();
    }

    public async Task<OrderSlip?> GetByIdAsync(int id)
    {
        return await _context.OrderSlips
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task AddSlipAsync(OrderSlip slip)
    {
        await _context.OrderSlips.AddAsync(slip);
    }

    public async Task UpdateSlipAsync(OrderSlip slip)
    {
        _context.OrderSlips.Update(slip);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        var slip = await _context.OrderSlips.FindAsync(id);
        if (slip != null) _context.OrderSlips.Remove(slip);
    }

    public async Task DeleteItemAsync(int itemId)
    {
        var item = await _context.OrderSlipItems.FindAsync(itemId);
        if (item != null) _context.OrderSlipItems.Remove(item);
    }

    public async Task<int> GetPendingCountAsync()
    {
        return await _context.OrderSlips.CountAsync(s => !s.IsReceived);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
