using StockSense.Application.DTOs;

namespace StockSense.Application.Interfaces;

public interface IPerformanceCalculator
{
    Task<BuildProjection> CalculateAsync(
        int bikeModelId,
        IReadOnlyCollection<int> partIds,
        CancellationToken cancellationToken = default);

    Task<BuildProjection> CalculateForStageAsync(
        int bikeModelId,
        int stageId,
        IReadOnlyCollection<int>? customPartIds = null,
        CancellationToken cancellationToken = default);

    Task<MaintenanceProjection> CalculateMaintenanceAsync(
        int bikeModelId,
        IReadOnlyCollection<int> partIds,
        CancellationToken cancellationToken = default);
}
