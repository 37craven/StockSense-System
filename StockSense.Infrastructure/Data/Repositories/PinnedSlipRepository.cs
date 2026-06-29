using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Repositories;

public class PinnedSlipRepository
{
    private readonly ApplicationDbContext _context;

    public PinnedSlipRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetSlipDataAsync(string userId)
    {
        var pinned = await _context.PinnedSlips
            .FirstOrDefaultAsync(p => p.UserId == userId);
        return pinned?.SlipData;
    }

    public async Task SetSlipDataAsync(string userId, string jsonData)
    {
        var existing = await _context.PinnedSlips
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (existing != null)
        {
            existing.SlipData = jsonData;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.PinnedSlips.Add(new PinnedSlip
            {
                UserId = userId,
                SlipData = jsonData,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }
}
