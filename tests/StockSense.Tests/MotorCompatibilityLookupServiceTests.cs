using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Services;

namespace StockSense.Tests;

public sealed class MotorCompatibilityLookupServiceTests
{
    [Fact]
    public async Task FindExact_filters_version_and_year_and_returns_stock_risk()
    {
        await using var context = CreateContext();
        var lowStockProduct = new Product
        {
            Name = "NMAX Air Filter",
            Category = "Air Filter",
            Brand = "Yamaha",
            Price = 450m,
            CurrentStock = 2,
            ReorderTarget = 5
        };
        var exact = CreateCompatibility("V2", 2021, null);
        exact.ProductMappings.Add(new ProductCompatibilityMapping
        {
            Product = lowStockProduct,
            PartFunction = "Air Filter",
            IsOEM = true
        });
        context.MotorCompatibilities.AddRange(
            exact,
            CreateCompatibility("V1", 2015, 2020),
            CreateCompatibility("V2", 2025, null));
        await context.SaveChangesAsync();

        var results = await new MotorCompatibilityLookupService(context).FindExactAsync(
            new MotorCompatibilityLookupQuery("yamaha", "nmax", "v2", 2024));

        var result = Assert.Single(results);
        Assert.Equal(exact.CompatibilityId, result.CompatibilityId);
        var product = Assert.Single(result.Products);
        Assert.Equal("LowStock", product.StockStatus);
        Assert.True(product.IsOem);
    }

    [Fact]
    public async Task FindExact_includes_open_ended_ranges_but_excludes_expired_ranges()
    {
        await using var context = CreateContext();
        context.MotorCompatibilities.AddRange(
            CreateCompatibility("Standard", 2020, 2022),
            CreateCompatibility("Standard", 2023, null));
        await context.SaveChangesAsync();

        var results = await new MotorCompatibilityLookupService(context).FindExactAsync(
            new MotorCompatibilityLookupQuery("Yamaha", "NMAX", "Standard", 2026));

        var result = Assert.Single(results);
        Assert.Equal(2023, result.YearStart);
        Assert.Null(result.YearEnd);
    }

    private static MotorCompatibility CreateCompatibility(
        string version, int yearStart, int? yearEnd) => new()
    {
        Manufacturer = "Yamaha",
        ModelName = "NMAX",
        VersionName = version,
        YearStart = yearStart,
        YearEnd = yearEnd
    };

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"compatibility-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
