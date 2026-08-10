using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSense.Infrastructure.Data;

namespace StockSense.Web.Controllers;

[ApiController]
[Route("api/admin-overrides")]
[Authorize(Roles = "Employee,Admin")]
public sealed class AdminOverridesController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("admins")]
    public async Task<IActionResult> GetActiveAdmins(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var admins = await (
            from user in context.Users.AsNoTracking()
            join userRole in context.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join role in context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where role.NormalizedName == "ADMIN"
                && (!user.LockoutEnd.HasValue || user.LockoutEnd <= now)
            let fullName = (user.FirstName + " " + user.LastName).Trim()
            orderby fullName, user.Id
            select new AdminOverrideOptionDto(user.Id, fullName == "" ? "Administrator" : fullName))
            .ToListAsync(cancellationToken);

        return Ok(admins);
    }
}

public sealed record AdminOverrideOptionDto(string Id, string DisplayName);
