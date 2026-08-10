using System.Security.Claims;
using StockSense.Application.DTOs;

namespace StockSense.Application.Interfaces;

public sealed record WorkOrderMutationResult(bool Succeeded, int StatusCode, string Message, decimal? TotalPrice = null);

public interface IBuildWorkOrderMutationService
{
    Task<WorkOrderMutationResult> UpdateStatusAsync(int id, UpdateWorkOrderStatusDto request, ClaimsPrincipal actor, CancellationToken cancellationToken = default);
    Task<WorkOrderMutationResult> UpdatePartsAsync(int id, UpdateBuildPartsDto request, ClaimsPrincipal actor, CancellationToken cancellationToken = default);
}
