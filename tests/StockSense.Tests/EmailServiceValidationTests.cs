using Microsoft.Extensions.Configuration;
using StockSense.Infrastructure.Services;

namespace StockSense.Tests;

public sealed class EmailServiceValidationTests
{
    [Fact]
    public async Task OrderEmailSender_rejects_missing_smtp_identity_before_network_access()
    {
        var sender = new OrderEmailSender(new ConfigurationBuilder().Build());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendEmailWithAttachmentAsync(
                "customer@example.com", "Order", "Body", [1, 2, 3], "order.pdf"));

        Assert.Equal("SMTP user is not configured.", exception.Message);
    }

    [Fact]
    public async Task EmailSender_rejects_malformed_recipient_before_network_access()
    {
        var sender = new EmailSender(new ConfigurationBuilder().Build());

        await Assert.ThrowsAnyAsync<Exception>(() =>
            sender.SendEmailAsync("not an email", "Subject", "Body"));
    }
}
