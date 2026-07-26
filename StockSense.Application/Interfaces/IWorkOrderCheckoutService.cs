using StockSense.Application.DTOs;

namespace StockSense.Application.Interfaces;

public interface IWorkOrderCheckoutService
{
    Task<ReceiptDto> CompleteAppointmentAsync(
        int appointmentId,
        CompleteWorkOrderDto request,
        string? employeeUserId,
        string locationId,
        CancellationToken cancellationToken = default);

    Task<ReceiptDto> CompleteBuildAsync(
        int buildId,
        CompleteWorkOrderDto request,
        string? employeeUserId,
        string locationId,
        CancellationToken cancellationToken = default);
}
