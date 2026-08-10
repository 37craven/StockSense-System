using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Data.Repositories;
using StockSense.Web.Controllers;

namespace StockSense.Tests;

public sealed class WorkOrderPermissionTests
{
    [Theory]
    [InlineData(WorkOrderStatuses.Confirmed, WorkOrderStatuses.Pending)]
    [InlineData(WorkOrderStatuses.Confirmed, WorkOrderStatuses.Cancelled)]
    [InlineData(WorkOrderStatuses.Completed, WorkOrderStatuses.Pending)]
    [InlineData(WorkOrderStatuses.Cancelled, WorkOrderStatuses.Pending)]
    public void EmployeeCannotPerformSensitiveTransition(string current, string target)
    {
        var error = WorkOrderRules.ValidateStatusTransition(current, target, isAdmin: false);

        Assert.Contains("admin", error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(WorkOrderStatuses.Pending, WorkOrderStatuses.Confirmed)]
    [InlineData(WorkOrderStatuses.Pending, WorkOrderStatuses.Cancelled)]
    public void EmployeeCanPerformPendingTransition(string current, string target)
    {
        Assert.Null(WorkOrderRules.ValidateStatusTransition(current, target, isAdmin: false));
    }

    [Theory]
    [InlineData(WorkOrderStatuses.Confirmed, WorkOrderStatuses.Pending)]
    [InlineData(WorkOrderStatuses.Confirmed, WorkOrderStatuses.Cancelled)]
    [InlineData(WorkOrderStatuses.Completed, WorkOrderStatuses.Pending)]
    [InlineData(WorkOrderStatuses.Cancelled, WorkOrderStatuses.Pending)]
    public void AdminCanPerformSensitiveTransition(string current, string target)
    {
        Assert.Null(WorkOrderRules.ValidateStatusTransition(current, target, isAdmin: true));
        Assert.True(WorkOrderRules.RequiresAdminReason(current, target));
    }

    [Theory]
    [InlineData("Build")]
    [InlineData("Appointment")]
    public async Task EmployeeIsDeniedConfirmedCancellation(string kind)
    {
        await using var db = NewDb();
        var id = await AddWorkOrder(db, kind, WorkOrderStatuses.Confirmed);

        var result = kind == "Build"
            ? await Builds(db, "Employee").UpdateStatus(id, Status(WorkOrderStatuses.Cancelled, "attempt"))
            : await Appointments(db, "Employee").UpdateStatus(id, Status(WorkOrderStatuses.Cancelled, "attempt"));

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(WorkOrderStatuses.Confirmed, await GetStatus(db, kind, id));
        Assert.Empty(db.WorkOrderAudits);
    }

    [Theory]
    [InlineData("Build")]
    [InlineData("Appointment")]
    public async Task AdminSensitiveTransitionRequiresReason(string kind)
    {
        await using var db = NewDb();
        var id = await AddWorkOrder(db, kind, WorkOrderStatuses.Confirmed);

        var result = kind == "Build"
            ? await Builds(db, "Admin").UpdateStatus(id, Status(WorkOrderStatuses.Cancelled))
            : await Appointments(db, "Admin").UpdateStatus(id, Status(WorkOrderStatuses.Cancelled));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(WorkOrderStatuses.Confirmed, await GetStatus(db, kind, id));
        Assert.Empty(db.WorkOrderAudits);
    }

    [Theory]
    [InlineData("Build")]
    [InlineData("Appointment")]
    public async Task AdminSensitiveTransitionPersistsTrimmedReasonAndIdentity(string kind)
    {
        await using var db = NewDb();
        var id = await AddWorkOrder(db, kind, WorkOrderStatuses.Confirmed);

        var result = kind == "Build"
            ? await Builds(db, "Admin").UpdateStatus(id, Status(WorkOrderStatuses.Cancelled, "  Customer requested cancellation  "))
            : await Appointments(db, "Admin").UpdateStatus(id, Status(WorkOrderStatuses.Cancelled, "  Customer requested cancellation  "));

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(WorkOrderStatuses.Cancelled, await GetStatus(db, kind, id));
        var audit = Assert.Single(db.WorkOrderAudits);
        Assert.Equal("Customer requested cancellation", audit.Reason);
        Assert.Equal("admin-1", audit.ActorUserId);
        Assert.Equal("Admin", audit.ActorRole);
        Assert.Equal(kind, audit.WorkOrderType);
    }

    [Fact]
    public async Task EmployeeCannotEditConfirmedBuildParts()
    {
        await using var db = NewDb();
        var id = await AddWorkOrder(db, "Build", WorkOrderStatuses.Confirmed);

        var result = await Builds(db, "Employee").UpdateParts(id, new UpdateBuildPartsDto());

        Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, ((ObjectResult)result).StatusCode);
    }

    [Fact]
    public async Task EmployeeCannotEditConfirmedAppointmentProductsOrPrices()
    {
        await using var db = NewDb();
        var id = await AddWorkOrder(db, "Appointment", WorkOrderStatuses.Confirmed);

        var result = await Appointments(db, "Employee").UpdateProducts(id,
            new AppointmentsController.UpdateAppointmentProductsDto { SelectedProductsJson = "[]", TotalAmount = 1m });

        Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, ((ObjectResult)result).StatusCode);
    }

    [Fact]
    public async Task EmployeeConfirmationWithUnavailablePartReturnsConflict()
    {
        await using var db = NewDb();
        var unavailable = new Product
        {
            Name = "Engine Oil", Category = "Parts", Brand = "Test", IsActive = true,
            CurrentStock = 1, ReservedStock = 1
        };
        db.Products.Add(unavailable);
        await db.SaveChangesAsync();
        var appointment = new Appointment
        {
            Category = "General", Status = WorkOrderStatuses.Pending,
            SelectedProductsJson = ProductsJson(unavailable.Id)
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var result = await Appointments(db, "Employee").UpdateStatus(
            appointment.Id, Status(WorkOrderStatuses.Confirmed));

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var responseText = System.Text.Json.JsonSerializer.Serialize(conflict.Value);
        Assert.Contains("Engine Oil", responseText);
        Assert.DoesNotContain("available", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(WorkOrderStatuses.Pending, appointment.Status);
        Assert.Equal(1, unavailable.ReservedStock);
        Assert.Empty(db.WorkOrderAudits);
    }

    [Fact]
    public async Task FailedConfirmationDoesNotPartiallyReserveAvailableParts()
    {
        await using var db = NewDb();
        var available = new Product
        {
            Name = "Filter", Category = "Parts", Brand = "Test", IsActive = true, CurrentStock = 2
        };
        var unavailable = new Product
        {
            Name = "Oil", Category = "Parts", Brand = "Test", IsActive = true, CurrentStock = 0
        };
        db.Products.AddRange(available, unavailable);
        await db.SaveChangesAsync();
        var appointment = new Appointment
        {
            Category = "General", Status = WorkOrderStatuses.Pending,
            SelectedProductsJson = ProductsJson(available.Id, unavailable.Id)
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var result = await Appointments(db, "Employee").UpdateStatus(
            appointment.Id, Status(WorkOrderStatuses.Confirmed));

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(0, available.ReservedStock);
        Assert.Equal(0, unavailable.ReservedStock);
        Assert.Equal(WorkOrderStatuses.Pending, appointment.Status);
    }

    private static string ProductsJson(params int[] productIds) =>
        System.Text.Json.JsonSerializer.Serialize(new[]
        {
            new
            {
                serviceName = "Service",
                servicePrice = 0,
                products = productIds.Select(id => new { id, name = "Part", price = 0, selected = true })
            }
        });

    private static UpdateWorkOrderStatusDto Status(string status, string? reason = null) =>
        new() { Status = status, Reason = reason };

    private static ApplicationDbContext NewDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static BuildsController Builds(ApplicationDbContext db, string role) => WithUser(new BuildsController(
        new BuildRequestRepository(db), null!, null!, db, new MotorcycleRepository(db),
        NullLogger<BuildsController>.Instance), role);

    private static AppointmentsController Appointments(ApplicationDbContext db, string role) => WithUser(new AppointmentsController(
        new AppointmentRepository(db), new StoreServiceRepository(db), null!, null!, db,
        new MotorcycleRepository(db), NullLogger<AppointmentsController>.Instance), role);

    private static T WithUser<T>(T controller, string role) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, role.Equals("Admin", StringComparison.Ordinal) ? "admin-1" : "employee-1"),
                     new Claim(ClaimTypes.Role, role)], "Test"))
            }
        };
        return controller;
    }

    private static async Task<int> AddWorkOrder(ApplicationDbContext db, string kind, string status)
    {
        if (kind == "Build")
        {
            var build = new BuildRequest { BuildName = "Test", Status = status, SelectedPartsJson = "[]" };
            db.BuildRequests.Add(build);
            await db.SaveChangesAsync();
            return build.Id;
        }

        var appointment = new Appointment { Category = "General", Status = status };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();
        return appointment.Id;
    }

    private static async Task<string> GetStatus(ApplicationDbContext db, string kind, int id)
    {
        db.ChangeTracker.Clear();
        return kind == "Build"
            ? (await db.BuildRequests.FindAsync(id))!.Status
            : (await db.Appointments.FindAsync(id))!.Status;
    }
}
