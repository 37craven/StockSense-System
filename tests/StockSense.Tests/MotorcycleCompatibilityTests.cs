using StockSense.Domain.Entities;
using StockSense.Web.Components.Pages.Admin;

namespace StockSense.Tests;

public sealed class MotorcycleCompatibilityTests
{
    [Fact]
    public void CollapseDuplicates_handles_legacy_rows_case_and_whitespace_insensitively()
    {
        var rows = new[]
        {
            new Motorcycle { Id = 9, Brand = " Honda ", Model = "Click", BaseCC = "125" },
            new Motorcycle { Id = 3, Brand = "honda", Model = " click ", BaseCC = "125 " },
            new Motorcycle { Id = 7, Brand = "Yamaha", Model = "NMAX", BaseCC = "155" }
        };

        var result = MotorcycleCompatibility.CollapseDuplicates(rows);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, motorcycle => motorcycle.Id == 3);
        Assert.DoesNotContain(result, motorcycle => motorcycle.Id == 9);
    }

    [Fact]
    public void CollapseDuplicates_is_deterministic_regardless_of_input_order()
    {
        var older = new Motorcycle { Id = 2, Brand = "Honda", Model = "PCX", BaseCC = "160" };
        var duplicate = new Motorcycle { Id = 8, Brand = "HONDA", Model = "PCX", BaseCC = "160" };

        Assert.Equal(2, Assert.Single(MotorcycleCompatibility.CollapseDuplicates([duplicate, older])).Id);
        Assert.Equal(2, Assert.Single(MotorcycleCompatibility.CollapseDuplicates([older, duplicate])).Id);
    }
}
