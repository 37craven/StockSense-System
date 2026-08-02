using StockSense.Application.DTOs;

namespace StockSense.Application.Interfaces;

public interface IMotorCompatibilityLookupService
{
    Task<IReadOnlyList<MotorCompatibilityDto>> FindExactAsync(
        MotorCompatibilityLookupQuery query,
        CancellationToken cancellationToken = default);
}
