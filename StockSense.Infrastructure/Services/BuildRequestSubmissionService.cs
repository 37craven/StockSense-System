using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

namespace StockSense.Infrastructure.Services;

public sealed class BuildRequestSubmissionService : IBuildRequestSubmissionService
{
    private readonly ApplicationDbContext _context;

    public BuildRequestSubmissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BuildRequest> QueueAsync(
        CreateBuildRequestDto request,
        BuildCustomerIdentity customer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var productIds = BuildPayloadParser.ParseProductIds(request.SelectedPartsJson);

        var products = await _context.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .Select(product => new { product.Id, product.Name, product.CurrentStock })
            .ToListAsync(cancellationToken);

        var foundIds = products.Select(product => product.Id).ToHashSet();
        var missingIds = productIds.Where(id => !foundIds.Contains(id)).ToArray();
        if (missingIds.Length > 0)
            throw new InvalidOperationException(
                $"One or more selected inventory products no longer exist: {string.Join(", ", missingIds)}.");

        var outOfStock = products.Where(product => product.CurrentStock <= 0).Select(product => product.Name).ToArray();
        if (outOfStock.Length > 0)
            throw new InvalidOperationException(
                $"Out-of-stock products are available for estimates only: {string.Join(", ", outOfStock)}.");

        var buildName = string.IsNullOrWhiteSpace(request.BuildName)
            ? "Custom Build"
            : request.BuildName.Trim();

        var build = new BuildRequest
        {
            CustomerName = customer.DisplayName,
            CustomerEmail = customer.Email,
            CustomerUserId = customer.UserId,
            BuildName = buildName,
            SelectedPartsJson = request.SelectedPartsJson,
            TotalPrice = request.TotalPrice,
            CreatedAt = DateTime.Now,
            Status = WorkOrderStatuses.Pending
        };

        _context.BuildRequests.Add(build);
        return build;
    }
}
