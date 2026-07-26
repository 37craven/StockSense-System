using StockSense.Domain.Entities;
using StockSense.Infrastructure.Services;

namespace StockSense.Tests;

public sealed class SafetyStockMathTests
{
    [Fact]
    public void Calculate_FewerThanThirtyDays_UsesColdStartPolicy()
    {
        var setting = CreateSetting(initialWeeklyDemand: 70m, leadTimeDays: 7, reviewPeriodDays: 7);

        var result = SafetyStockMath.Calculate(setting, Repeat(0m, 29), []);

        Assert.Equal(InventoryCalculationStages.ColdStart, result.Stage);
        Assert.Equal(InventoryConfidenceLevels.Low, result.Confidence);
        Assert.Equal(10m, result.AppliedAverageDailyDemand);
        Assert.Equal(70, result.SafetyStock);
        Assert.Equal(140, result.ReorderPoint);
        Assert.Equal(210, result.TargetStock);
    }

    [Fact]
    public void Calculate_ThirtyToFiftyNineDays_UsesFiftyFiftyBlend()
    {
        var setting = CreateSetting(initialWeeklyDemand: 14m, leadTimeDays: 7, reviewPeriodDays: 7);

        var result = SafetyStockMath.Calculate(setting, Repeat(4m, 30), []);

        Assert.Equal(InventoryCalculationStages.Learning, result.Stage);
        Assert.Equal(3m, result.AppliedAverageDailyDemand);
        Assert.Equal(0m, result.DemandStandardDeviation);
        Assert.Equal(21, result.ReorderPoint);
        Assert.Equal(42, result.TargetStock);
    }

    [Fact]
    public void Calculate_SixtyToEightyNineDays_UsesSeventyThirtyBlendAndCeiling()
    {
        var setting = CreateSetting(initialWeeklyDemand: 14m, leadTimeDays: 7, reviewPeriodDays: 7);

        var result = SafetyStockMath.Calculate(setting, Repeat(4m, 60), []);

        Assert.Equal(InventoryCalculationStages.Learning, result.Stage);
        Assert.Equal(3.4m, result.AppliedAverageDailyDemand);
        Assert.Equal(24, result.ReorderPoint);
        Assert.Equal(48, result.TargetStock);
    }

    [Fact]
    public void Calculate_DataDrivenWithoutLeadHistory_UsesFixedLeadTimeFormula()
    {
        var setting = CreateSetting(leadTimeDays: 4, reviewPeriodDays: 3);

        var result = SafetyStockMath.Calculate(setting, AlternatingDemand(90), []);

        Assert.Equal(InventoryCalculationStages.DataDriven, result.Stage);
        Assert.Equal(InventoryConfidenceLevels.Medium, result.Confidence);
        Assert.Equal(1m, result.AppliedAverageDailyDemand);
        Assert.Equal(1m, result.DemandStandardDeviation);
        Assert.Equal(4m, result.AverageLeadTimeDays);
        Assert.Equal(0m, result.LeadTimeStandardDeviation);
        Assert.Equal(4, result.SafetyStock);
        Assert.Equal(8, result.ReorderPoint);
        Assert.Equal(11, result.TargetStock);
    }

    [Fact]
    public void Calculate_DataDrivenWithFiveLeadTimes_UsesCombinedVariabilityFormula()
    {
        var setting = CreateSetting(leadTimeDays: 4, reviewPeriodDays: 3);
        decimal[] leadTimes = [2m, 4m, 6m, 8m, 10m];

        var result = SafetyStockMath.Calculate(setting, AlternatingDemand(90), leadTimes);

        Assert.Equal(InventoryConfidenceLevels.High, result.Confidence);
        Assert.Equal(6m, result.AverageLeadTimeDays);
        Assert.InRange(result.LeadTimeStandardDeviation, 2.8284m, 2.8285m);
        Assert.Equal(7, result.SafetyStock);
        Assert.Equal(13, result.ReorderPoint);
        Assert.Equal(16, result.TargetStock);
    }

    [Fact]
    public void PopulationStandardDeviation_ZeroOrSingleObservation_ReturnsZero()
    {
        Assert.Equal(0m, SafetyStockMath.PopulationStandardDeviation([]));
        Assert.Equal(0m, SafetyStockMath.PopulationStandardDeviation([42m]));
    }

    [Fact]
    public void Calculate_EnforcesMinimumSafetyStock()
    {
        var setting = CreateSetting(minimumSafetyStock: 5);

        var result = SafetyStockMath.Calculate(setting, Repeat(2m, 90), []);

        Assert.Equal(5, result.SafetyStock);
    }

    [Fact]
    public void Calculate_EnforcesMaximumSafetyStock()
    {
        var setting = CreateSetting(maximumSafetyStock: 5, leadTimeDays: 4);

        var result = SafetyStockMath.Calculate(setting, AlternatingDemand(90), [2m, 4m, 6m, 8m, 10m]);

        Assert.Equal(5, result.SafetyStock);
    }

    [Fact]
    public void Calculate_FractionalColdStartQuantities_AlwaysRoundUp()
    {
        var setting = CreateSetting(
            initialWeeklyDemand: 1m,
            leadTimeDays: 1,
            reviewPeriodDays: 1,
            bufferDays: 1);

        var result = SafetyStockMath.Calculate(setting, [0m], []);

        Assert.Equal(1, result.SafetyStock);
        Assert.Equal(2, result.ReorderPoint);
        Assert.Equal(2, result.TargetStock);
    }

    [Fact]
    public void ResolveZScore_ReturnsEverySupportedMapping()
    {
        Assert.Equal(1.2816m, SafetyStockMath.ResolveZScore(0.9000m));
        Assert.Equal(1.6449m, SafetyStockMath.ResolveZScore(0.9500m));
        Assert.Equal(1.9600m, SafetyStockMath.ResolveZScore(0.9750m));
        Assert.Equal(2.0537m, SafetyStockMath.ResolveZScore(0.9800m));
        Assert.Equal(2.3263m, SafetyStockMath.ResolveZScore(0.9900m));
    }

    [Fact]
    public void ResolveZScore_RejectsUnsupportedServiceLevel()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => SafetyStockMath.ResolveZScore(0.9600m));

        Assert.Contains("unsupported", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Calculate_RejectsNegativeDemand()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => SafetyStockMath.Calculate(CreateSetting(), [0m, -1m], []));

        Assert.Contains("cannot be negative", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Calculate_RejectsNonPositiveLeadTimeObservation()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => SafetyStockMath.Calculate(CreateSetting(), Repeat(1m, 90), [2m, 3m, 0m, 4m, 5m]));

        Assert.Contains("must be positive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Calculate_RejectsEmptyDemandSeries()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => SafetyStockMath.Calculate(CreateSetting(), [], []));

        Assert.Contains("usable demand day", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateSetting_RejectsInvalidNumericLimits()
    {
        var setting = CreateSetting();
        setting.MinimumSafetyStock = 10;
        setting.MaximumSafetyStock = 9;

        var exception = Assert.Throws<InvalidOperationException>(
            () => SafetyStockMath.ValidateSetting(setting));

        Assert.Contains("Maximum safety stock", exception.Message, StringComparison.Ordinal);
    }

    private static ProductInventorySetting CreateSetting(
        decimal initialWeeklyDemand = 0m,
        int leadTimeDays = 7,
        int reviewPeriodDays = 7,
        int bufferDays = 7,
        int minimumSafetyStock = 0,
        int? maximumSafetyStock = null) =>
        new()
        {
            InitialEstimatedWeeklyDemand = initialWeeklyDemand,
            DefaultLeadTimeDays = leadTimeDays,
            ReviewPeriodDays = reviewPeriodDays,
            BufferDays = bufferDays,
            ServiceLevel = 0.9500m,
            MinimumSafetyStock = minimumSafetyStock,
            MaximumSafetyStock = maximumSafetyStock,
            MinimumOrderQuantity = 1,
            PackageSize = 1,
            CalculationMode = InventoryCalculationModes.Auto
        };

    private static decimal[] Repeat(decimal value, int count) =>
        Enumerable.Repeat(value, count).ToArray();

    private static decimal[] AlternatingDemand(int count) =>
        Enumerable.Range(0, count).Select(index => index % 2 == 0 ? 0m : 2m).ToArray();
}
