using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Infrastructure.Data;
using StockSense.Web.Helpers;

namespace StockSense.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private static readonly string[] AllowedRoles = ["Customer", "Employee", "Admin"];
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext? _context;
        private readonly IAdminPinService? _adminPinService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext? context = null,
            IAdminPinService? adminPinService = null)
        {
            _userManager = userManager;
            _context = context;
            _adminPinService = adminPinService;
        }

        [HttpPost("my-pin")]
        public async Task<IActionResult> SetMyAdminPin(
            [FromBody] SetAdminPinDto dto,
            CancellationToken cancellationToken = default)
        {
            if (!string.Equals(dto.NewPin, dto.ConfirmPin, StringComparison.Ordinal))
                return BadRequest(ApiResponse.Error("The new PIN and confirmation do not match."));

            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized(ApiResponse.Error("Please sign in again to update your admin PIN."));

            if (_adminPinService is null)
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    ApiResponse.Error("Admin PIN settings are temporarily unavailable."));

            var result = await _adminPinService.SetPinAsync(
                currentUserId,
                dto.CurrentPassword,
                dto.NewPin,
                cancellationToken);

            return result.Succeeded
                ? Ok(ApiResponse.Success("Your admin PIN has been updated."))
                : BadRequest(ApiResponse.Error(result.Error ?? "Your admin PIN could not be updated."));
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var currentUserId = _userManager.GetUserId(User);
            var users = await _userManager.Users
                .Select(u => new UserDto
                {
                    Id = u.Id, Email = u.Email ?? "",
                    FullName = $"{u.FirstName} {u.LastName}",
                    Role = u.Role,
                    IsBlocked = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow,
                    IsCurrentUser = u.Id == currentUserId
                })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPost("create-employee")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.Email, Email = dto.Email, EmailConfirmed = true,
                FirstName = dto.FirstName, LastName = dto.LastName, Role = dto.Role
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse.Error(IdentityErrorFeedback.GetUserMessage(result.Errors)));
            }

            try
            {
                var roleResult = await _userManager.AddToRoleAsync(user, dto.Role);
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    var roleError = roleResult.Errors.FirstOrDefault()?.Description
                        ?? $"Could not assign role '{dto.Role}'. Ensure the role exists in the system.";
                    return BadRequest(ApiResponse.Error(roleError));
                }
            }
            catch (InvalidOperationException)
            {
                await _userManager.DeleteAsync(user);
                return BadRequest(ApiResponse.Error($"Role '{dto.Role}' does not exist in the system. Contact an administrator to seed roles."));
            }

            return Ok();
        }

        [HttpPost("change-role")]
        public async Task<IActionResult> ChangeRole(
            [FromBody] RoleChangeRequest req, CancellationToken cancellationToken = default)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.Equals(req.UserId, currentUserId, StringComparison.OrdinalIgnoreCase))
                return BadRequest(ApiResponse.Error("You cannot change your own admin role."));

            var targetRole = AllowedRoles.FirstOrDefault(
                role => string.Equals(role, req.NewRole?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (targetRole is null)
                return BadRequest(ApiResponse.Error("Role must be Customer, Employee, or Admin."));

            if (_context is not null)
            {
                var normalizedRole = targetRole.ToUpperInvariant();
                var roleExists = await _context.Roles.AsNoTracking()
                    .AnyAsync(role => role.NormalizedName == normalizedRole, cancellationToken);
                if (!roleExists)
                    return BadRequest(ApiResponse.Error($"The {targetRole} role is not available."));
            }

            var user = await _userManager.FindByIdAsync(req.UserId);
            if (user == null) return NotFound(ApiResponse.NotFound("User"));

            var currentRoles = (await _userManager.GetRolesAsync(user)).ToArray();
            var originalRoleProperty = user.Role;
            if (currentRoles.Length == 1
                && string.Equals(currentRoles[0], targetRole, StringComparison.OrdinalIgnoreCase)
                && string.Equals(user.Role, targetRole, StringComparison.Ordinal))
                return Ok();

            if (_context is null)
                return await ApplyRoleChangeAsync(
                    user, currentRoles, originalRoleProperty, targetRole, cancellationToken);

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(() => ApplyRoleChangeAsync(
                user, currentRoles, originalRoleProperty, targetRole, cancellationToken));
        }

        private async Task<IActionResult> ApplyRoleChangeAsync(
            ApplicationUser user,
            IReadOnlyCollection<string> currentRoles,
            string originalRoleProperty,
            string targetRole,
            CancellationToken cancellationToken)
        {
            await using var transaction = _context is null
                ? null
                : await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (currentRoles.Count > 0)
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                        return await RoleChangeFailureAsync(
                            transaction, user, currentRoles, originalRoleProperty, targetRole, "remove existing roles", removeResult);
                }

                var addResult = await _userManager.AddToRoleAsync(user, targetRole);
                if (!addResult.Succeeded)
                    return await RoleChangeFailureAsync(
                        transaction, user, currentRoles, originalRoleProperty, targetRole, $"assign the {targetRole} role", addResult);

                user.Role = targetRole;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    return await RoleChangeFailureAsync(
                        transaction, user, currentRoles, originalRoleProperty, targetRole, "update the user role", updateResult);

                if (transaction is not null) await transaction.CommitAsync(cancellationToken);
                return Ok();
            }
            catch (Exception)
            {
                user.Role = originalRoleProperty;
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _context!.ChangeTracker.Clear();
                }
                else
                {
                    if (!await RestoreRolesAsync(user, currentRoles, targetRole))
                        return BadRequest(ApiResponse.Error("The role change failed and the original role could not be restored."));
                }
                return BadRequest(ApiResponse.Error("The role change could not be completed. No changes were saved."));
            }
        }

        private async Task<IActionResult> RoleChangeFailureAsync(
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction,
            ApplicationUser user,
            IReadOnlyCollection<string> originalRoles,
            string originalRoleProperty,
            string targetRole,
            string operation,
            IdentityResult result)
        {
            user.Role = originalRoleProperty;
            if (transaction is not null)
            {
                await transaction.RollbackAsync();
                _context!.ChangeTracker.Clear();
            }
            else
            {
                if (!await RestoreRolesAsync(user, originalRoles, targetRole))
                    return BadRequest(ApiResponse.Error("The role change failed and the original role could not be restored."));
            }
            var detail = result.Errors.FirstOrDefault()?.Description;
            return BadRequest(ApiResponse.Error(
                string.IsNullOrWhiteSpace(detail)
                    ? $"Could not {operation}. No role changes were saved."
                    : $"Could not {operation}: {detail} No role changes were saved."));
        }

        private async Task<bool> RestoreRolesAsync(
            ApplicationUser user, IReadOnlyCollection<string> originalRoles, string targetRole)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any(role => string.Equals(role, targetRole, StringComparison.OrdinalIgnoreCase)))
            {
                var removeResult = await _userManager.RemoveFromRoleAsync(user, targetRole);
                if (!removeResult.Succeeded) return false;
            }
            foreach (var originalRole in originalRoles)
                if (!roles.Any(role => string.Equals(role, originalRole, StringComparison.OrdinalIgnoreCase)))
                {
                    var addResult = await _userManager.AddToRoleAsync(user, originalRole);
                    if (!addResult.Succeeded) return false;
                }
            return true;
        }

        [HttpPost("toggle-block/{id}")]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.Equals(id, currentUserId, StringComparison.OrdinalIgnoreCase))
                return BadRequest(ApiResponse.Error("You cannot block or unblock your own admin account."));

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(ApiResponse.NotFound("User"));

            var result = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow
                ? await _userManager.SetLockoutEndDateAsync(user, null)
                : await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            if (result.Succeeded) return Ok();
            return BadRequest(ApiResponse.Error(
                result.Errors.FirstOrDefault()?.Description ?? "The account status could not be changed."));
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.Equals(id, currentUserId, StringComparison.OrdinalIgnoreCase))
                return BadRequest(ApiResponse.Error("You cannot delete your own admin account."));

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(ApiResponse.NotFound("User"));

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded) return Ok(ApiResponse.Success("User deleted successfully"));

            return BadRequest(ApiResponse.Error("Failed to delete user."));
        }
    }

    public class RoleChangeRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string NewRole { get; set; } = string.Empty;
    }
}
