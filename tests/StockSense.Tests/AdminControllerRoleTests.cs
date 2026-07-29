using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using StockSense.Infrastructure.Data;
using StockSense.Web.Controllers;

namespace StockSense.Tests;

public sealed class AdminControllerRoleTests
{
    [Fact]
    public async Task ChangeRole_RejectsAuthenticatedAdminsOwnUserId()
    {
        var user = new ApplicationUser { Id = "admin-1", UserName = "admin@example.test", Role = "Admin" };
        using var fixture = new Fixture(user, "Admin");
        var controller = fixture.ControllerFor(user.Id);

        var result = await controller.ChangeRole(new RoleChangeRequest
        {
            UserId = user.Id.ToUpperInvariant(),
            NewRole = "Employee"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("own admin role", badRequest.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Admin", user.Role);
        Assert.Equal(["Admin"], await fixture.Manager.GetRolesAsync(user));
    }

    [Fact]
    public async Task ChangeRole_AllowsAdminToUpdateAnotherUser()
    {
        var employee = new ApplicationUser { Id = "employee-1", UserName = "employee@example.test", Role = "Employee" };
        using var fixture = new Fixture(employee, "Employee");
        var controller = fixture.ControllerFor("admin-1");

        var result = await controller.ChangeRole(new RoleChangeRequest
        {
            UserId = employee.Id,
            NewRole = "Admin"
        });

        Assert.IsType<OkResult>(result);
        Assert.Equal("Admin", employee.Role);
        Assert.Equal("Admin", Assert.Single(await fixture.Manager.GetRolesAsync(employee)), ignoreCase: true);
    }

    [Fact]
    public async Task ChangeRole_InvalidRoleLeavesMembershipAndPropertyUnchanged()
    {
        var employee = new ApplicationUser { Id = "employee-1", UserName = "employee@example.test", Role = "Employee" };
        using var fixture = new Fixture(employee, "Employee");

        var result = await fixture.ControllerFor("admin-1").ChangeRole(new RoleChangeRequest
        {
            UserId = employee.Id,
            NewRole = "SuperAdmin"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Customer, Employee, or Admin", badRequest.Value!.ToString(), StringComparison.Ordinal);
        Assert.Equal("Employee", employee.Role);
        Assert.Equal("Employee", Assert.Single(await fixture.Manager.GetRolesAsync(employee)));
    }

    [Fact]
    public async Task ChangeRole_AddFailureRestoresOriginalMembershipAndProperty()
    {
        var employee = new ApplicationUser { Id = "employee-1", UserName = "employee@example.test", Role = "Employee" };
        using var fixture = new Fixture(employee, "Employee", failRole: "ADMIN");

        var result = await fixture.ControllerFor("admin-1").ChangeRole(new RoleChangeRequest
        {
            UserId = employee.Id,
            NewRole = "Admin"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("No changes were saved", badRequest.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Employee", employee.Role);
        Assert.Equal("Employee", Assert.Single(await fixture.Manager.GetRolesAsync(employee)), ignoreCase: true);
    }

    [Fact]
    public async Task ToggleBlock_RejectsCaseVariantAuthenticatedUserId()
    {
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin@example.test", Role = "Admin", LockoutEnabled = true };
        using var fixture = new Fixture(admin, "Admin");

        var result = await fixture.ControllerFor(admin.Id).ToggleBlock(admin.Id.ToUpperInvariant());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("own admin account", badRequest.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Null(admin.LockoutEnd);
    }

    [Fact]
    public async Task ToggleBlock_AllowsAdminToChangeAnotherUsersStatus()
    {
        var employee = new ApplicationUser { Id = "employee-1", UserName = "employee@example.test", Role = "Employee", LockoutEnabled = true };
        using var fixture = new Fixture(employee, "Employee");

        var result = await fixture.ControllerFor("admin-1").ToggleBlock(employee.Id);

        Assert.IsType<OkResult>(result);
        Assert.True(employee.LockoutEnd > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task DeleteUser_RejectsCaseVariantAuthenticatedUserId()
    {
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin@example.test", Role = "Admin" };
        using var fixture = new Fixture(admin, "Admin");

        var result = await fixture.ControllerFor(admin.Id).DeleteUser(admin.Id.ToUpperInvariant());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("own admin account", badRequest.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(await fixture.Manager.FindByIdAsync(admin.Id));
    }

    [Fact]
    public async Task DeleteUser_AllowsAdminToDeleteAnotherUser()
    {
        var employee = new ApplicationUser { Id = "employee-1", UserName = "employee@example.test", Role = "Employee" };
        using var fixture = new Fixture(employee, "Employee");

        var result = await fixture.ControllerFor("admin-1").DeleteUser(employee.Id);

        Assert.IsType<OkObjectResult>(result);
        Assert.Null(await fixture.Manager.FindByIdAsync(employee.Id));
    }

    private sealed class Fixture : IDisposable
    {
        private readonly TestUserStore _store;

        public Fixture(ApplicationUser user, string role, string? failRole = null)
        {
            _store = new TestUserStore(user, role, failRole);
            Manager = new UserManager<ApplicationUser>(
                _store,
                Options.Create(new IdentityOptions()),
                new PasswordHasher<ApplicationUser>(),
                [],
                [],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new ServiceCollection().BuildServiceProvider(),
                NullLogger<UserManager<ApplicationUser>>.Instance);
        }

        public UserManager<ApplicationUser> Manager { get; }

        public AdminController ControllerFor(string authenticatedUserId) => new(Manager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, authenticatedUserId)], "Test"))
                }
            }
        };

        public void Dispose()
        {
            Manager.Dispose();
            _store.Dispose();
        }
    }

    private sealed class TestUserStore : IUserRoleStore<ApplicationUser>, IUserLockoutStore<ApplicationUser>
    {
        private readonly Dictionary<string, ApplicationUser> _users;
        private readonly Dictionary<string, HashSet<string>> _roles;
        private readonly string? _failRole;

        public TestUserStore(ApplicationUser user, string role, string? failRole)
        {
            _users = new() { [user.Id] = user };
            _roles = new() { [user.Id] = new(StringComparer.OrdinalIgnoreCase) { role } };
            _failRole = failRole;
        }

        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            _users[user.Id] = user;
            _roles[user.Id] = new(StringComparer.OrdinalIgnoreCase);
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            _users[user.Id] = user;
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            _users.Remove(user.Id);
            _roles.Remove(user.Id);
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult(_users.GetValueOrDefault(userId));

        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            Task.FromResult(_users.Values.FirstOrDefault(user => user.NormalizedUserName == normalizedUserName));

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) { user.UserName = userName; return Task.CompletedTask; }
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }

        public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            if (string.Equals(roleName, _failRole, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Role assignment failed for {roleName}.");
            _roles[user.Id].Add(roleName);
            return Task.CompletedTask;
        }

        public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            _roles[user.Id].Remove(roleName);
            return Task.CompletedTask;
        }

        public Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult<IList<string>>(_roles[user.Id].ToList());

        public Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) =>
            Task.FromResult(_roles[user.Id].Contains(roleName));

        public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken) =>
            Task.FromResult<IList<ApplicationUser>>(_users.Values.Where(user => _roles[user.Id].Contains(roleName)).ToList());

        public Task<DateTimeOffset?> GetLockoutEndDateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.LockoutEnd);

        public Task SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        {
            user.LockoutEnd = lockoutEnd;
            return Task.CompletedTask;
        }

        public Task<int> IncrementAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount++;
            return Task.FromResult(user.AccessFailedCount);
        }

        public Task ResetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount = 0;
            return Task.CompletedTask;
        }

        public Task<int> GetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.AccessFailedCount);

        public Task<bool> GetLockoutEnabledAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.LockoutEnabled);

        public Task SetLockoutEnabledAsync(ApplicationUser user, bool enabled, CancellationToken cancellationToken)
        {
            user.LockoutEnabled = enabled;
            return Task.CompletedTask;
        }

        public void Dispose() { }
    }
}
