using Microsoft.AspNetCore.Identity;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Services;

namespace StockSense.Web.Components.Account;

internal sealed class IdentityEmailAdapter : IEmailSender<ApplicationUser>
{
    private readonly EmailSender _inner;

    public IdentityEmailAdapter(EmailSender inner) => _inner = inner;

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        _inner.SendConfirmationLinkAsync(user, email, confirmationLink);

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        _inner.SendPasswordResetLinkAsync(user, email, resetLink);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        _inner.SendPasswordResetCodeAsync(user, email, resetCode);
}
