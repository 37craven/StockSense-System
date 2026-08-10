using Microsoft.EntityFrameworkCore;
using StockSense.Infrastructure.Data;

namespace StockSense.Web.Services;

internal static class StockAvailabilityValidator
{
    public static async Task<string?> ValidateAsync(
        ApplicationDbContext context,
        IEnumerable<int> productIds,
        string bookingName,
        CancellationToken cancellationToken = default)
    {
        var requested = productIds
            .Where(id => id > 0)
            .GroupBy(id => id)
            .ToDictionary(group => group.Key, group => group.Count());
        if (requested.Count == 0) return null;

        var ids = requested.Keys.ToList();
        var products = await context.Products
            .AsNoTracking()
            .Where(product => ids.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        var unavailable = requested
            .Where(item => !products.TryGetValue(item.Key, out var product)
                || !product.IsActive
                || product.AvailableStock < item.Value)
            .Select(item => products.TryGetValue(item.Key, out var product)
                ? product.Name
                : $"part #{item.Key} (no longer available)")
            .ToList();

        return unavailable.Count == 0
            ? null
            : $"This {bookingName} cannot be submitted because there is not enough stock for: {string.Join(", ", unavailable)}. Please choose other parts or wait until they are restocked.";
    }
}
