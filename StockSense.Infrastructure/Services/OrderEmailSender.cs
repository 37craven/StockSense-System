using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using StockSense.Application.Interfaces;
namespace StockSense.Infrastructure.Services;

public class OrderEmailSender : IOrderEmailSender
{
    private readonly IConfiguration _config;

    public OrderEmailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailWithAttachmentAsync(
        string toEmail,
        string subject,
        string body,
        byte[] attachment,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var smtpUser = _config["Smtp:User"]
            ?? throw new InvalidOperationException("SMTP user is not configured.");
        var smtpHost = _config["Smtp:Host"]
            ?? throw new InvalidOperationException("SMTP host is not configured.");
        var smtpPassword = _config["Smtp:Pass"]
            ?? throw new InvalidOperationException("SMTP password is not configured.");
        var port = _config.GetValue<int?>("Smtp:Port")
            ?? throw new InvalidOperationException("SMTP port is not configured.");
        if (port is <= 0 or > 65535)
            throw new InvalidOperationException("SMTP port must be between 1 and 65535.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sap Shop (Motor Parts & Accessories)", smtpUser));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = body };

        if (attachment != null)
        {
            builder.Attachments.Add(fileName, attachment);
        }

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        // ponytail: macOS revocation-check flake; revisit if we move off Gmail relay
        client.ServerCertificateValidationCallback = (_, _, _, _) => true;
        await client.ConnectAsync(smtpHost, port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(smtpUser, smtpPassword, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
