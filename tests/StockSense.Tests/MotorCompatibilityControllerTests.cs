using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Web.Controllers;

namespace StockSense.Tests;

public sealed class MotorCompatibilityControllerTests
{
    [Fact]
    public void Controller_requires_inventory_staff_policy()
    {
        var attribute = typeof(MotorCompatibilityController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("InventoryStaff", attribute.Policy);
    }

    [Theory]
    [InlineData(null, "NMAX", "V2", 2024)]
    [InlineData("Yamaha", null, "V2", 2024)]
    [InlineData("Yamaha", "NMAX", null, 2024)]
    [InlineData("Yamaha", "NMAX", "V2", null)]
    public async Task FindExact_requires_full_version_aware_vehicle_identity(
        string? manufacturer, string? modelName, string? versionName, int? year)
    {
        var service = new StubLookupService();
        var controller = new MotorCompatibilityController(service);

        var result = await controller.FindExact(
            manufacturer, modelName, versionName, year, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(service.WasCalled);
    }

    [Fact]
    public async Task FindExact_trims_query_and_returns_inventory_matches()
    {
        var expected = new MotorCompatibilityDto(
            7, "Yamaha", "NMAX", "V2", 2021, null,
            "10W-40", null, null, "CPR8EA-9", null, "B65", null, null,
            null, null, null, "B6H-E4451-00",
            [new CompatibleProductDto(
                12, "NMAX Air Filter", "Air Filter", "Yamaha", 450m,
                2, 5, "LowStock", "/images/filter.webp", "Air Filter", true, null)]);
        var service = new StubLookupService { Results = [expected] };
        var controller = new MotorCompatibilityController(service);

        var result = await controller.FindExact(
            " Yamaha ", " NMAX ", " V2 ", 2024, default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(service.Results, ok.Value);
        Assert.Equal(new MotorCompatibilityLookupQuery("Yamaha", "NMAX", "V2", 2024), service.Query);
    }

    [Fact]
    public async Task FindExact_returns_not_found_when_no_local_mapping_exists()
    {
        var controller = new MotorCompatibilityController(new StubLookupService());

        var result = await controller.FindExact(
            "Yamaha", "NMAX", "V2", 2024, default);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    private sealed class StubLookupService : IMotorCompatibilityLookupService
    {
        public IReadOnlyList<MotorCompatibilityDto> Results { get; init; } = [];
        public bool WasCalled { get; private set; }
        public MotorCompatibilityLookupQuery? Query { get; private set; }

        public Task<IReadOnlyList<MotorCompatibilityDto>> FindExactAsync(
            MotorCompatibilityLookupQuery query,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            Query = query;
            return Task.FromResult(Results);
        }
    }
}
