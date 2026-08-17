namespace StockSense.Application.Interfaces;

public interface IOrderEmailSender
{
    Task SendEmailWithAttachmentAsync(
        string toEmail,
        string subject,
        string body,
        byte[] attachment,
        string fileName,
        CancellationToken cancellationToken = default);
}
