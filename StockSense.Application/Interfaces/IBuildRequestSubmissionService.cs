using StockSense.Application.DTOs;
using StockSense.Domain.Entities;

namespace StockSense.Application.Interfaces;

public interface IBuildRequestSubmissionService
{
    Task<BuildRequest> QueueAsync(
        CreateBuildRequestDto request,
        BuildCustomerIdentity customer,
        CancellationToken cancellationToken = default);
}
