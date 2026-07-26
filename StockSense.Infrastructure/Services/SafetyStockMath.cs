using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Services;

public sealed record SafetyStockPolicyResult(
    string Stage,
    string Confidence,
    decimal AppliedAverageDailyDemand,
    decimal DemandStandardDeviation,
    decimal AverageLeadTimeDays,
    decimal LeadTimeStandardDeviation,
    int SafetyStock,
    int ReorderPoint,
    int TargetStock,
    decimal ZScore,
    bool ManualOverrideUsed,
    string Explanation);

public static class SafetyStockMath
{
    private static readonly IReadOnlyDictionary<decimal, decimal> SupportedZScores =
        new Dictionary<decimal, decimal>
        {
            [0.9000m] = 1.2816m,
            [0.9500m] = 1.6449m,
            [0.9750m] = 1.9600m,
            [0.9800m] = 2.0537m,
            [0.9900m] = 2.3263m
        };

    public static decimal ResolveZScore(decimal serviceLevel)
    {
        if (SupportedZScores.TryGetValue(serviceLevel, out var zScore))
            return zScore;

        throw new InvalidOperationException(
            $"Service level {serviceLevel:0.0000} is unsupported. Use 0.9000, 0.9500, 0.9750, 0.9800, or 0.9900.");
    }

    public static decimal PopulationStandardDeviation(IReadOnlyList<decimal> values)
    {
        if (values.Count < 2)
            return 0m;

        var average = values.Average();
        var variance = values.Sum(value => (value - average) * (value - average)) / values.Count;
        var result = Math.Sqrt((double)variance);
        if (double.IsNaN(result) || double.IsInfinity(result))
            throw new InvalidOperationException("The statistical calculation produced an invalid value.");
        return (decimal)result;
    }

    public static SafetyStockPolicyResult Calculate(
        ProductInventorySetting setting,
        IReadOnlyList<decimal> dailyDemand,
        IReadOnlyList<decimal> validLeadTimes)
    {
        ValidateSetting(setting);
        if (dailyDemand.Count == 0)
            throw new InvalidOperationException("At least one usable demand day is required.");
        if (dailyDemand.Any(value => value < 0))
            throw new InvalidOperationException("Demand values cannot be negative.");
        if (validLeadTimes.Any(value => value <= 0))
            throw new InvalidOperationException("Lead-time observations must be positive.");

        var usableDays = dailyDemand.Count;
        var observedAverage = dailyDemand.Average();
        var demandDeviation = PopulationStandardDeviation(dailyDemand);
        var coldStartAverage = setting.InitialEstimatedWeeklyDemand / 7m;
        var zScore = ResolveZScore(setting.ServiceLevel);

        var observedLeadTimeAvailable = validLeadTimes.Count >= 5;
        var observedAverageLeadTime = observedLeadTimeAvailable ? validLeadTimes.Average() : 0m;
        var observedLeadTimeDeviation = observedLeadTimeAvailable
            ? PopulationStandardDeviation(validLeadTimes)
            : 0m;
        decimal averageLeadTime = setting.DefaultLeadTimeDays;
        decimal leadTimeDeviation = 0m;

        string stage;
        string confidence;
        string explanation;
        decimal appliedAverage;
        int safetyStock;

        if (usableDays < 30)
        {
            stage = InventoryCalculationStages.ColdStart;
            confidence = InventoryConfidenceLevels.Low;
            appliedAverage = coldStartAverage;
            safetyStock = CeilingToInt(appliedAverage * setting.BufferDays);
            explanation = "Cold-start calculation used because fewer than 30 usable daily records are available.";
        }
        else if (usableDays < 90)
        {
            stage = InventoryCalculationStages.Learning;
            confidence = InventoryConfidenceLevels.Medium;
            var observedWeight = usableDays < 60 ? 0.50m : 0.70m;
            appliedAverage = observedWeight * observedAverage + (1m - observedWeight) * coldStartAverage;
            safetyStock = CeilingToInt(zScore * demandDeviation * Sqrt(setting.DefaultLeadTimeDays));
            explanation = usableDays < 60
                ? "Learning calculation blended observed demand and the cold-start estimate at 50/50."
                : "Learning calculation blended observed demand and the cold-start estimate at 70/30.";
        }
        else
        {
            stage = InventoryCalculationStages.DataDriven;
            confidence = observedLeadTimeAvailable
                ? InventoryConfidenceLevels.High
                : InventoryConfidenceLevels.Medium;
            appliedAverage = observedAverage;
            if (observedLeadTimeAvailable)
            {
                averageLeadTime = observedAverageLeadTime;
                leadTimeDeviation = observedLeadTimeDeviation;
            }
            safetyStock = observedLeadTimeAvailable
                ? CeilingToInt(zScore * Sqrt(
                    averageLeadTime * demandDeviation * demandDeviation
                    + appliedAverage * appliedAverage * leadTimeDeviation * leadTimeDeviation))
                : CeilingToInt(zScore * demandDeviation * Sqrt(setting.DefaultLeadTimeDays));
            explanation = observedLeadTimeAvailable
                ? "Data-driven calculation used complete demand history and observed supplier lead-time variability."
                : "Data-driven calculation used complete demand history and the configured lead time because fewer than five completed supplier orders are available.";
        }

        safetyStock = ApplySafetyLimits(safetyStock, setting);
        var reorderPoint = CeilingToInt(appliedAverage * averageLeadTime + safetyStock);
        var targetStock = CeilingToInt(appliedAverage * (averageLeadTime + setting.ReviewPeriodDays) + safetyStock);
        var manualOverrideUsed = false;

        if (string.Equals(setting.CalculationMode, InventoryCalculationModes.Manual, StringComparison.Ordinal))
        {
            stage = InventoryCalculationStages.Manual;
            confidence = usableDays >= 90 ? confidence : InventoryConfidenceLevels.Low;
            appliedAverage = observedAverage;
            if (setting.ManualSafetyStock.HasValue)
            {
                safetyStock = ApplySafetyLimits(setting.ManualSafetyStock.Value, setting);
                manualOverrideUsed = true;
            }

            reorderPoint = setting.ManualReorderPoint
                ?? CeilingToInt(appliedAverage * averageLeadTime + safetyStock);
            manualOverrideUsed |= setting.ManualReorderPoint.HasValue;
            targetStock = CeilingToInt(appliedAverage * (averageLeadTime + setting.ReviewPeriodDays) + safetyStock);
            explanation = "Manual inventory-policy values were applied; observed demand and lead-time metrics are shown for reference.";
        }

        if (reorderPoint < 0)
            throw new InvalidOperationException("The reorder point cannot be negative.");
        targetStock = Math.Max(targetStock, reorderPoint);
        if (setting.MaximumStockLevel.HasValue)
        {
            if (setting.MaximumStockLevel.Value < reorderPoint)
                throw new InvalidOperationException("Maximum stock level cannot be lower than the applied reorder point.");
            targetStock = Math.Min(targetStock, setting.MaximumStockLevel.Value);
        }

        return new SafetyStockPolicyResult(
            stage,
            confidence,
            appliedAverage,
            demandDeviation,
            averageLeadTime,
            leadTimeDeviation,
            safetyStock,
            reorderPoint,
            targetStock,
            zScore,
            manualOverrideUsed,
            explanation);
    }

    public static void ValidateSetting(ProductInventorySetting setting)
    {
        if (setting.InitialEstimatedWeeklyDemand < 0 || setting.DefaultLeadTimeDays < 1
            || setting.ReviewPeriodDays < 1 || setting.BufferDays < 0
            || setting.MinimumSafetyStock < 0 || setting.MinimumOrderQuantity < 1
            || setting.PackageSize < 1)
            throw new InvalidOperationException("Inventory settings contain a negative value or a value below its required minimum.");
        if (setting.MaximumSafetyStock < setting.MinimumSafetyStock)
            throw new InvalidOperationException("Maximum safety stock cannot be lower than minimum safety stock.");
        if (setting.MaximumStockLevel is <= 0)
            throw new InvalidOperationException("Maximum stock level must be greater than zero when provided.");
        if (setting.ManualSafetyStock is < 0 || setting.ManualReorderPoint is < 0)
            throw new InvalidOperationException("Manual safety stock and reorder point cannot be negative.");
        if (setting.ServiceLevel is < 0.5000m or > 0.9990m)
            throw new InvalidOperationException("Service level must be between 0.5000 and 0.9990.");
        if (!string.Equals(setting.CalculationMode, InventoryCalculationModes.Auto, StringComparison.Ordinal)
            && !string.Equals(setting.CalculationMode, InventoryCalculationModes.Manual, StringComparison.Ordinal))
            throw new InvalidOperationException("Calculation mode must be Auto or Manual.");
        ResolveZScore(setting.ServiceLevel);
    }

    private static int ApplySafetyLimits(int value, ProductInventorySetting setting)
    {
        var limited = Math.Max(0, Math.Max(value, setting.MinimumSafetyStock));
        return setting.MaximumSafetyStock.HasValue
            ? Math.Min(limited, setting.MaximumSafetyStock.Value)
            : limited;
    }

    private static int CeilingToInt(decimal value)
    {
        if (value < 0 || value > int.MaxValue)
            throw new InvalidOperationException("The calculated stock quantity is outside the supported range.");
        return decimal.ToInt32(decimal.Ceiling(value));
    }

    private static decimal Sqrt(decimal value)
    {
        var result = Math.Sqrt((double)value);
        if (double.IsNaN(result) || double.IsInfinity(result))
            throw new InvalidOperationException("The statistical calculation produced an invalid value.");
        return (decimal)result;
    }
}
