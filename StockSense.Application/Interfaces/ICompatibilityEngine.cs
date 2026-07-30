using StockSense.Application.DTOs;
using StockSense.Domain.Entities;

namespace StockSense.Application.Interfaces;

public interface ICompatibilityEngine
{
    Task<ValidationResult> ValidateBuildAsync(
        int bikeModelId,
        IReadOnlyCollection<int> partIds,
        int? stageId = null,
        CancellationToken cancellationToken = default);

    Task<List<UpgradePart>> GetCompatiblePartsAsync(
        int bikeModelId,
        int categoryId,
        IReadOnlyCollection<int> alreadySelected,
        CancellationToken cancellationToken = default);
}
