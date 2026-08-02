using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Repositories;

public class BuildRequestRepository
{
    private readonly ApplicationDbContext _context;

    public BuildRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BuildRequest>> GetAllAsync()
    {
        return await _context.BuildRequests
            .Include(b => b.Transaction)
            .Include(b => b.Motorcycle)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<BuildRequest?> GetByIdAsync(int id)
    {
        return await _context.BuildRequests.FindAsync(id);
    }

    public async Task<List<BuildRequest>> GetByCustomerNameAsync(string customerName)
    {
        return await _context.BuildRequests
            .Include(b => b.Transaction)
            .Include(b => b.Motorcycle)
            .Where(b => b.CustomerName == customerName)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<BuildRequest>> GetByCustomerIdentityAsync(
        string userId,
        string email,
        string fullName)
    {
        return await _context.BuildRequests
            .Include(b => b.Transaction)
            .Include(b => b.Motorcycle)
            .Where(b => b.CustomerUserId == userId
                || (b.CustomerUserId == null && b.CustomerEmail == email)
                || (b.CustomerUserId == null && b.CustomerEmail == null
                    && (b.CustomerName == email || b.CustomerName == fullName)))
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(BuildRequest request)
    {
        _context.BuildRequests.Add(request);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
