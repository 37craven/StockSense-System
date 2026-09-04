using Microsoft.AspNetCore.Identity;

namespace StockSense.Infrastructure.Data;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Admin, Employee, or Customer

    public bool IsTrusted { get; set; }

    // Stored as an ASP.NET Identity password hash; the PIN itself is never persisted.
    public string? AdminPinHash { get; set; }
    public int AdminPinFailedAccessCount { get; set; }
    public DateTimeOffset? AdminPinLockoutEnd { get; set; }
}
