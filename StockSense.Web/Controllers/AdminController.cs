using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Infrastructure.Data;

namespace StockSense.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users
                .Select(u => new UserDto
                {
                    Id = u.Id, Email = u.Email ?? "",
                    FullName = $"{u.FirstName} {u.LastName}",
                    Role = u.Role,
                    IsBlocked = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow
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
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, dto.Role);
                return Ok();
            }

            var firstError = result.Errors.FirstOrDefault()?.Description ?? "Registration failed";
            return BadRequest(ApiResponse.Error(firstError));
        }

        [HttpPost("change-role")]
        public async Task<IActionResult> ChangeRole([FromBody] RoleChangeRequest req)
        {
            var user = await _userManager.FindByIdAsync(req.UserId);
            if (user == null) return NotFound(ApiResponse.NotFound("User"));

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, req.NewRole);

            user.Role = req.NewRole;
            await _userManager.UpdateAsync(user);
            return Ok();
        }

        [HttpPost("toggle-block/{id}")]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(ApiResponse.NotFound("User"));

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
                await _userManager.SetLockoutEndDateAsync(user, null);
            else
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

            return Ok();
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(ApiResponse.NotFound("User"));

            var currentUserId = _userManager.GetUserId(User);
            if (id == currentUserId)
                return BadRequest(ApiResponse.Error("You cannot delete your own admin account."));

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
