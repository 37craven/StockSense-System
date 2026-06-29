using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
namespace StockSense.Infrastructure.Services;

public class OrderEmailSender
{
    private readonly IConfiguration _config;

    public OrderEmailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachment, string fileName)
    {
        var smtpUser = _config["Smtp:User"];
        if (string.IsNullOrEmpty(smtpUser))
            throw new InvalidOperationException("SMTP user is not configured.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("StockSense Orders", smtpUser));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = body };

        if (attachment != null)
        {
            builder.Attachments.Add(fileName, attachment);
        }

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        int port = _config.GetValue<int>("Smtp:Port");

        await client.ConnectAsync(_config["Smtp:Host"], port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(smtpUser, _config["Smtp:Pass"]);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}