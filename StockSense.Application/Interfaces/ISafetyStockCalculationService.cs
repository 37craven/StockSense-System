using StockSense.Application.DTOs;

namespace StockSense.Application.Interfaces;

public interface ISafetyStockCalculationService
{
    Task<SafetyStockCalculationResult> RecalculateProductAsync(
        int productId,
        string locationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SafetyStockCalculationResult>> RecalculateProductsAsync(
        IEnumerable<int> productIds,
        string locationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SafetyStockCalculationResult>> RecalculateAllAsync(
        string locationId,
        CancellationToken cancellationToken = default);
}
