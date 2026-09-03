using StockSense.Application.DTOs;

namespace StockSense.Application.Interfaces;

public interface IWorkOrderEmailSender
{
    Task SendStatusEmailAsync(
        string toEmail,
        string customerName,
        string workOrderType,
        string status,
        int workOrderId,
        string summary,
        ReceiptDto? receipt = null,
        CancellationToken cancellationToken = default);

    Task SendRescheduleEmailAsync(
        string toEmail,
        string customerName,
        int appointmentId,
        string oldDate,
        string newDate,
        string mechanic,
        CancellationToken cancellationToken = default);
}
