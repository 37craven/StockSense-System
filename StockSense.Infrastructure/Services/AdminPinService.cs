using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.Interfaces;
using StockSense.Infrastructure.Data;

namespace StockSense.Infrastructure.Services;

public sealed class AdminPinService : IAdminPinService
{
    public const int PinLength = 6;
    public const int MaximumFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<ApplicationUser> _hasher;
    private readonly TimeProvider _timeProvider;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminPinService(
        ApplicationDbContext context,
        IPasswordHasher<ApplicationUser> hasher,
        TimeProvider timeProvider,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _hasher = hasher;
        _timeProvider = timeProvider;
        _userManager = userManager;
    }

    public async Task<AdminPinOperationResult> SetPinAsync(
        string adminUserId,
        string currentPassword,
        string newPin,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidPin(newPin))
            return new(false, "The admin PIN must contain exactly 6 numbers.");

        var admin = await _context.Users.SingleOrDefaultAsync(
            user => user.Id == adminUserId,
            cancellationToken);

        if (admin is null || !await _userManager.IsInRoleAsync(admin, "Admin"))
            return new(false, "Admin account not found.");

        if (string.IsNullOrWhiteSpace(currentPassword) ||
            string.IsNullOrWhiteSpace(admin.PasswordHash) ||
            _hasher.VerifyHashedPassword(admin, admin.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
        {
            return new(false, "Your current password is incorrect.");
        }

        admin.AdminPinHash = _hasher.HashPassword(admin, newPin);
        admin.AdminPinFailedAccessCount = 0;
        admin.AdminPinLockoutEnd = null;
        await _context.SaveChangesAsync(cancellationToken);

        return new(true);
    }

    public async Task<AdminPinVerificationResult> VerifyAsync(
        string adminEmail,
        string pin,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = adminEmail?.Trim().ToUpperInvariant();
        var admin = string.IsNullOrWhiteSpace(normalizedEmail)
            ? null
            : await _context.Users.SingleOrDefaultAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken);

        return await VerifyAdminAsync(admin, pin, cancellationToken);
    }

    public async Task<AdminPinVerificationResult> VerifyByUserIdAsync(
        string adminUserId,
        string pin,
        CancellationToken cancellationToken = default)
    {
        var id = adminUserId?.Trim();
        var admin = string.IsNullOrWhiteSpace(id)
            ? null
            : await _context.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

        return await VerifyAdminAsync(admin, pin, cancellationToken);
    }

    private async Task<AdminPinVerificationResult> VerifyAdminAsync(
        ApplicationUser? admin,
        string pin,
        CancellationToken cancellationToken)
    {
        // Keep a generic response for invalid selections, users who are no longer
        // admins, unset PINs, and incorrect PINs.
        if (admin is null || !await _userManager.IsInRoleAsync(admin, "Admin") || string.IsNullOrWhiteSpace(admin.AdminPinHash))
            return InvalidCredentials();

        if (admin.LockoutEnd is { } accountLockout && accountLockout > _timeProvider.GetUtcNow())
            return InvalidCredentials();

        var now = _timeProvider.GetUtcNow();
        if (admin.AdminPinLockoutEnd is { } lockedUntil && lockedUntil > now)
            return new(false, Error: "Too many incorrect attempts. Please try again later.", LockedUntil: lockedUntil);

        var verification = IsValidPin(pin)
            ? _hasher.VerifyHashedPassword(admin, admin.AdminPinHash, pin)
            : PasswordVerificationResult.Failed;

        if (verification == PasswordVerificationResult.Failed)
        {
            admin.AdminPinFailedAccessCount++;
            if (admin.AdminPinFailedAccessCount >= MaximumFailedAttempts)
            {
                admin.AdminPinFailedAccessCount = 0;
                admin.AdminPinLockoutEnd = now.Add(LockoutDuration);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return admin.AdminPinLockoutEnd is { } newLockout && newLockout > now
                ? new(false, Error: "Too many incorrect attempts. Please try again later.", LockedUntil: newLockout)
                : InvalidCredentials();
        }

        admin.AdminPinFailedAccessCount = 0;
        admin.AdminPinLockoutEnd = null;

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            admin.AdminPinHash = _hasher.HashPassword(admin, pin);

        await _context.SaveChangesAsync(cancellationToken);
        return new(true, admin.Id, admin.Email);
    }

    private static bool IsValidPin(string? pin) =>
        pin is { Length: PinLength } && pin.All(char.IsAsciiDigit);

    private static AdminPinVerificationResult InvalidCredentials() =>
        new(false, Error: "The selected admin or PIN is incorrect.");
}
