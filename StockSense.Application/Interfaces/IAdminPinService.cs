namespace StockSense.Application.Interfaces;

public interface IAdminPinService
{
    Task<AdminPinOperationResult> SetPinAsync(
        string adminUserId,
        string currentPassword,
        string newPin,
        CancellationToken cancellationToken = default);

    Task<AdminPinVerificationResult> VerifyAsync(
        string adminEmail,
        string pin,
        CancellationToken cancellationToken = default);

    Task<AdminPinVerificationResult> VerifyByUserIdAsync(
        string adminUserId,
        string pin,
        CancellationToken cancellationToken = default);
}

public sealed record AdminPinOperationResult(bool Succeeded, string? Error = null);

public sealed record AdminPinVerificationResult(
    bool Succeeded,
    string? AdminUserId = null,
    string? AdminEmail = null,
    string? Error = null,
    DateTimeOffset? LockedUntil = null);
