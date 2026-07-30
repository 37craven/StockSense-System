using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

namespace StockSense.Infrastructure.Services;

public sealed class PerformanceCalculator : IPerformanceCalculator
{
    private const decimal LaborRatePerHour = 500m;
    private readonly ApplicationDbContext _context;
    private readonly ICompatibilityEngine _compatibility;

    public PerformanceCalculator(
        ApplicationDbContext context,
        ICompatibilityEngine compatibility)
    {
        _context = context;
        _compatibility = compatibility;
    }

    public async Task<BuildProjection> CalculateAsync(
        int bikeModelId,
        IReadOnlyCollection<int> partIds,
        CancellationToken cancellationToken = default)
    {
        var bike = await GetBikeAsync(bikeModelId, cancellationToken);
        var parts = await GetPartsAsync(partIds, cancellationToken);
        var validation = await _compatibility.ValidateBuildAsync(
            bikeModelId,
            partIds,
            cancellationToken: cancellationToken);
        return await ComputeProjectionAsync(bike, parts, validation, cancellationToken);
    }

    public async Task<BuildProjection> CalculateForStageAsync(
        int bikeModelId,
        int stageId,
        IReadOnlyCollection<int>? customPartIds = null,
        CancellationToken cancellationToken = default)
    {
        var stage = await _context.UpgradeStages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == stageId && item.BikeModelId == bikeModelId && item.IsActive,
                cancellationToken)
            ?? throw new ArgumentException("Build package not found for this motorcycle.", nameof(stageId));

        var partIds = customPartIds is { Count: > 0 }
            ? customPartIds
            : DeserializeIntList(stage.RecommendedPartIdsJson);
        var bike = await GetBikeAsync(bikeModelId, cancellationToken);
        var parts = await GetPartsAsync(partIds, cancellationToken);
        var validation = await _compatibility.ValidateBuildAsync(
            bikeModelId,
            partIds,
            stageId,
            cancellationToken);
        return await ComputeProjectionAsync(bike, parts, validation, cancellationToken);
    }


    private async Task<BuildProjection> ComputeProjectionAsync(
        BikeModel bike,
        List<UpgradePart> parts,
        ValidationResult validation,
        CancellationToken cancellationToken)
    {
        var bonuses = GetSynergyBonuses(parts);
        var projection = new BuildProjection
        {
            BikeModelId = bike.Id,
            BikeName = bike.DisplayName,
            BaseCC = bike.BaseCC,
            BaseHP = bike.BaseHP,
            BaseTorque = bike.BaseTorque,
            AddedCC = parts.Sum(part => part.CCGain),
            AddedHP = parts.Sum(part => part.HPGain) + bonuses.HP,
            AddedTorque = parts.Sum(part => part.TorqueGain) + bonuses.Torque,
            ReliabilityScore = Math.Clamp(
                100 + parts.Sum(part => part.ReliabilityImpact) + bonuses.Reliability,
                0,
                100),
            TotalPartsCost = parts.Sum(part =>
                part.ListPrice > 0 ? part.ListPrice : part.Product.Price),
            EstimatedLaborCost = parts.Sum(part => part.EstimatedLaborHours * LaborRatePerHour),
            ValidationErrors = validation.Errors,
            ValidationSuggestions = validation.Suggestions,
            IsValid = validation.IsValid
        };

        projection.FinalCC = projection.BaseCC + projection.AddedCC;
        projection.FinalHP = projection.BaseHP + projection.AddedHP;
        projection.FinalTorque = projection.BaseTorque + projection.AddedTorque;
        projection.TotalCost = projection.TotalPartsCost + projection.EstimatedLaborCost;

        var matchedStage = await _context.UpgradeStages
            .AsNoTracking()
            .Where(stage => stage.BikeModelId == bike.Id && stage.IsActive)
            .OrderBy(stage => Math.Abs(stage.TargetCC - projection.FinalCC))
            .FirstOrDefaultAsync(cancellationToken);
        if (matchedStage is not null)
        {
            projection.MatchedStageName = matchedStage.Name;
            projection.MatchedStageNumber = matchedStage.StageNumber;
        }

        return projection;
    }


    private static (int HP, int Torque, int Reliability) GetSynergyBonuses(List<UpgradePart> parts)
    {
        var hasBlock = parts.Any(part => HasCategory(part, "Block"));
        var hasCrank = parts.Any(part => HasCategory(part, "Crank"));
        var hasHead = parts.Any(part => HasCategory(part, "Head"));
        var hasThrottle = parts.Any(part => HasCategory(part, "Throttle"));
        var hasPipe = parts.Any(part => HasCategory(part, "Pipe") || HasCategory(part, "Exhaust"));
        var hasEcu = parts.Any(part => HasCategory(part, "ECU"));

        var hp = 0;
        var torque = 0;
        var reliability = 0;
        if (hasBlock && hasCrank)
        {
            hp += 2;
            torque += 3;
            reliability -= 5;
        }
        if (hasHead && hasThrottle && hasPipe && hasEcu)
        {
            hp += 2;
            torque += 1;
        }
        if (hasBlock && hasCrank && hasHead && hasThrottle && hasPipe && hasEcu)
            reliability -= 10;
        return (hp, torque, reliability);
    }

    private async Task<BikeModel> GetBikeAsync(int bikeModelId, CancellationToken cancellationToken) =>
        await _context.BikeModels
            .AsNoTracking()
            .FirstOrDefaultAsync(model => model.Id == bikeModelId && model.IsActive, cancellationToken)
        ?? throw new ArgumentException("Motorcycle model not found.", nameof(bikeModelId));

    private async Task<List<UpgradePart>> GetPartsAsync(
        IReadOnlyCollection<int> partIds,
        CancellationToken cancellationToken)
    {
        var ids = partIds.Distinct().ToArray();
        return await _context.UpgradeParts
            .AsNoTracking()
            .Include(part => part.Category)
            .Include(part => part.Product)
            .Where(part => ids.Contains(part.Id) && part.IsActive)
            .ToListAsync(cancellationToken);
    }

    private static bool HasCategory(UpgradePart part, string term) =>
        part.Category.Name.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static List<int> DeserializeIntList(string? json)
    {
        try { return JsonSerializer.Deserialize<List<int>>(json ?? "[]") ?? []; }
        catch (JsonException) { return []; }
    }
}
