using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSense.Infrastructure.Data;
using StockSense.Web.Controllers;

namespace StockSense.Tests;

public sealed class AdminOverridesControllerTests
{
    [Fact]
    public async Task GetActiveAdmins_ReturnsOnlyActiveUsersWithAdminMembership()
    {
        await using var context = CreateContext();
        var adminRole = new IdentityRole { Id = "role-admin", Name = "Admin", NormalizedName = "ADMIN" };
        var employeeRole = new IdentityRole { Id = "role-employee", Name = "Employee", NormalizedName = "EMPLOYEE" };
        context.Roles.AddRange(adminRole, employeeRole);
        context.Users.AddRange(
            new ApplicationUser { Id = "active-admin", FirstName = "Ana", LastName = "Santos", UserName = "ana", Role = "Admin" },
            new ApplicationUser { Id = "blocked-admin", FirstName = "Ben", LastName = "Cruz", UserName = "ben", Role = "Admin", LockoutEnd = DateTimeOffset.UtcNow.AddDays(1) },
            new ApplicationUser { Id = "employee", FirstName = "Cara", LastName = "Reyes", UserName = "cara", Role = "Employee" });
        context.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = "active-admin", RoleId = adminRole.Id },
            new IdentityUserRole<string> { UserId = "blocked-admin", RoleId = adminRole.Id },
            new IdentityUserRole<string> { UserId = "employee", RoleId = employeeRole.Id });
        await context.SaveChangesAsync();

        var result = await new AdminOverridesController(context).GetActiveAdmins(default);

        var ok = Assert.IsType<OkObjectResult>(result);
        var option = Assert.Single(Assert.IsAssignableFrom<IEnumerable<AdminOverrideOptionDto>>(ok.Value));
        Assert.Equal("active-admin", option.Id);
        Assert.Equal("Ana Santos", option.DisplayName);
    }

    [Fact]
    public void Controller_AllowsOnlyEmployeesAndAdmins()
    {
        var attribute = Assert.Single(typeof(AdminOverridesController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal("Employee,Admin", attribute.Roles);
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
}
