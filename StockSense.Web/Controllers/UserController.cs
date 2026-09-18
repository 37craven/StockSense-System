using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StockSense.Infrastructure.Data;
using StockSense.Application.DTOs;

namespace StockSense.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public UserController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return Ok(new UserProfileDto
        {
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            FullName = string.IsNullOrWhiteSpace(fullName)
                ? user.Email?.Split('@')[0] ?? "Customer"
                : fullName,
            Email = user.Email ?? string.Empty
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var firstName = dto.FirstName.Trim();
        var lastName = dto.LastName.Trim();

        if (string.Equals(user.FirstName, firstName, StringComparison.Ordinal) &&
            string.Equals(user.LastName, lastName, StringComparison.Ordinal))
        {
            var currentFullName = $"{user.FirstName} {user.LastName}".Trim();
            return Ok(new UserProfileDto
            {
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                FullName = string.IsNullOrWhiteSpace(currentFullName) ? user.Email?.Split('@')[0] ?? "Customer" : currentFullName,
                Email = user.Email ?? string.Empty
            });
        }

        user.FirstName = firstName;
        user.LastName = lastName;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        // Propagate new display name to all owned appointments/builds (all statuses)
        try
        {
            await _context.Appointments.Where(a => a.CustomerUserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.CustomerName, fullName));
            await _context.BuildRequests.Where(b => b.CustomerUserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.CustomerName, fullName));
        }
        catch { /* best-effort: profile already updated */ }

        return Ok(new UserProfileDto
        {
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            FullName = string.IsNullOrWhiteSpace(fullName) ? user.Email?.Split('@')[0] ?? "Customer" : fullName,
            Email = user.Email ?? string.Empty
        });
    }
}
