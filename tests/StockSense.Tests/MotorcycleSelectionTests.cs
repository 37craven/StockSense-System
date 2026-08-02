using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Data.Repositories;
using StockSense.Web.Controllers;

namespace StockSense.Tests;

public sealed class MotorcycleSelectionTests
{
    [Fact]
    public void New_work_order_requests_require_an_existing_positive_motorcycle_id()
    {
        var appointment = new CreateAppointmentDto
        {
            CustomerName = "Customer",
            AppointmentDate = DateTime.Today,
            TimeSlot = "09:00",
            SelectedServices = ["Oil Change"]
        };
        var build = new CreateBuildRequestDto
        {
            CustomerName = "Customer",
            SelectedPartsJson = "[]"
        };

        Assert.Contains(Validate(appointment), result => result.MemberNames.Contains(nameof(CreateAppointmentDto.MotorcycleId)));
        Assert.Contains(Validate(build), result => result.MemberNames.Contains(nameof(CreateBuildRequestDto.MotorcycleId)));

        appointment.MotorcycleId = 0;
        build.MotorcycleId = 0;
        Assert.Contains(Validate(appointment), result => result.MemberNames.Contains(nameof(CreateAppointmentDto.MotorcycleId)));
        Assert.Contains(Validate(build), result => result.MemberNames.Contains(nameof(CreateBuildRequestDto.MotorcycleId)));

        appointment.MotorcycleId = 1;
        build.MotorcycleId = 1;
        Assert.DoesNotContain(Validate(appointment), result => result.MemberNames.Contains(nameof(CreateAppointmentDto.MotorcycleId)));
        Assert.DoesNotContain(Validate(build), result => result.MemberNames.Contains(nameof(CreateBuildRequestDto.MotorcycleId)));
    }

    [Fact]
    public async Task Repository_exposes_existing_motorcycles_and_validates_by_id()
    {
        await using var context = CreateContext();
        context.Motorcycles.AddRange(
            new Motorcycle { Id = 1, Brand = "Honda", Model = "Click", BaseCC = "125cc" },
            new Motorcycle { Id = 2, Brand = "Yamaha", Model = "NMAX", BaseCC = "155cc" });
        await context.SaveChangesAsync();
        var repository = new MotorcycleRepository(context);

        var available = await repository.GetSelectableAsync();

        Assert.Equal(2, available.Count);
        Assert.NotNull(await repository.GetSelectableByIdAsync(1));
        Assert.NotNull(await repository.GetSelectableByIdAsync(2));
        Assert.Null(await repository.GetSelectableByIdAsync(999));
    }

    [Fact]
    public async Task Motorcycle_options_endpoint_is_authenticated_and_returns_existing_records()
    {
        var authorization = typeof(MotorcyclesController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorization);

        await using var context = CreateContext();
        context.Motorcycles.AddRange(
            new Motorcycle { Brand = "Kawasaki", Model = "Barako", BaseCC = "175cc" });
        await context.SaveChangesAsync();
        var controller = new MotorcyclesController(new MotorcycleRepository(context));

        var result = await controller.GetSelectable(default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var option = Assert.Single(Assert.IsType<List<MotorcycleOptionDto>>(ok.Value));
        Assert.Equal("Kawasaki Barako (175cc)", option.DisplayName);
    }

    [Fact]
    public async Task Work_order_queries_include_readable_motorcycle_details()
    {
        await using var context = CreateContext();
        var motorcycle = new Motorcycle { Brand = "Suzuki", Model = "Burgman Street", BaseCC = "125cc" };
        context.Appointments.Add(new Appointment
        {
            CustomerName = "Customer",
            CustomerUserId = "user-1",
            Motorcycle = motorcycle
        });
        context.BuildRequests.Add(new BuildRequest
        {
            CustomerName = "Customer",
            CustomerUserId = "user-1",
            Motorcycle = motorcycle
        });
        await context.SaveChangesAsync();

        var appointments = await new AppointmentRepository(context)
            .GetByCustomerIdentityAsync("user-1", "customer@example.com", "Customer");
        var builds = await new BuildRequestRepository(context)
            .GetByCustomerIdentityAsync("user-1", "customer@example.com", "Customer");

        Assert.Equal("Burgman Street", Assert.Single(appointments).Motorcycle?.Model);
        Assert.Equal("Burgman Street", Assert.Single(builds).Motorcycle?.Model);
    }

    private static List<ValidationResult> Validate(object value)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true);
        return results;
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
