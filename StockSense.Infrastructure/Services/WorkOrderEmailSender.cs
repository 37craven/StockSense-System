using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;

namespace StockSense.Infrastructure.Services;

public class WorkOrderEmailSender : IWorkOrderEmailSender
{
    private readonly EmailSender _emailSender;
    private readonly OrderEmailSender _orderEmailSender;
    private readonly DocumentService _documentService;
    private readonly IConfiguration _config;
    private readonly ILogger<WorkOrderEmailSender> _logger;

    public WorkOrderEmailSender(
        EmailSender emailSender,
        OrderEmailSender orderEmailSender,
        DocumentService documentService,
        IConfiguration config,
        ILogger<WorkOrderEmailSender> logger)
    {
        _emailSender = emailSender;
        _orderEmailSender = orderEmailSender;
        _documentService = documentService;
        _config = config;
        _logger = logger;
    }

    public async Task SendStatusEmailAsync(
        string toEmail,
        string customerName,
        string workOrderType,
        string status,
        int workOrderId,
        string summary,
        ReceiptDto? receipt = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return;

        try
        {
            var baseUrl = _config["App:BaseUrl"] ?? "https://localhost:5001";
            var label = workOrderType == "Appointment" ? "appointment" : "build";

            var html = status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase)
                ? WorkOrderEmailTemplate.BuildConfirmed(customerName, workOrderType, workOrderId, summary, baseUrl)
                : WorkOrderEmailTemplate.BuildCompleted(customerName, workOrderType, workOrderId, summary, baseUrl);

            var subject = status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase)
                ? $"Sap Shop - Your {label} has been confirmed"
                : $"Sap Shop - Your {label} is complete";

            if (receipt is not null)
            {
                var pdfBytes = _documentService.GenerateTransactionReceiptPdf(receipt);
                var fileName = $"Receipt_{receipt.InvoiceNumber}.pdf";
                await _orderEmailSender.SendEmailWithAttachmentAsync(toEmail, subject, html, pdfBytes, fileName, cancellationToken);
            }
            else
            {
                await _emailSender.SendEmailAsync(toEmail, subject, html);
            }

            _logger.LogInformation("Sent {Status} email for {Type} #{Id} to {Email}.", status, workOrderType, workOrderId, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send {Status} email for {Type} #{Id}.", status, workOrderType, workOrderId);
        }
    }

    public async Task SendRescheduleEmailAsync(
        string toEmail,
        string customerName,
        int appointmentId,
        string oldDate,
        string newDate,
        string mechanic,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return;

        try
        {
            var baseUrl = _config["App:BaseUrl"] ?? "https://localhost:5001";
            var html = WorkOrderEmailTemplate.BuildRescheduled(customerName, appointmentId, oldDate, newDate, mechanic, baseUrl);
            await _emailSender.SendEmailAsync(toEmail, "Sap Shop - Your appointment has been rescheduled", html);
            _logger.LogInformation("Sent reschedule email for appointment #{Id} to {Email}.", appointmentId, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send reschedule email for appointment #{Id}.", appointmentId);
        }
    }
}
