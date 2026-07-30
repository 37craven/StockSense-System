using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

namespace StockSense.Infrastructure.Services;

public sealed class CompatibilityEngine : ICompatibilityEngine
{
    private readonly ApplicationDbContext _context;

    public CompatibilityEngine(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ValidationResult> ValidateBuildAsync(
        int bikeModelId,
        IReadOnlyCollection<int> partIds,
        int? stageId = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();
        var requestedIds = partIds.Distinct().ToArray();
        var bike = await _context.BikeModels
            .AsNoTracking()
            .FirstOrDefaultAsync(model => model.Id == bikeModelId && model.IsActive, cancellationToken);

        if (bike is null)
        {
            result.Errors.Add("Invalid or inactive motorcycle model selected.");
            return result;
        }

        var parts = await _context.UpgradeParts
            .AsNoTracking()
            .Include(part => part.Category)
            .Include(part => part.Product)
            .Where(part => requestedIds.Contains(part.Id) && part.IsActive)
            .ToListAsync(cancellationToken);

        var foundIds = parts.Select(part => part.Id).ToHashSet();
        var missingIds = requestedIds.Where(id => !foundIds.Contains(id)).ToArray();
        if (missingIds.Length > 0)
            result.Errors.Add($"Parts not found or inactive: {string.Join(", ", missingIds)}.");

        foreach (var part in parts)
        {
            if (!IsCompatibleWithBike(part, bike))
                result.Errors.Add($"{part.Product.Name} is not compatible with {bike.DisplayName}.");
            if (part.Product.CurrentStock <= 0)
                result.Warnings.Add($"{part.Product.Name} is out of stock and can only be used for an estimate.");
        }

        var selectedIds = foundIds;
        foreach (var part in parts)
        {
            foreach (var requiredId in DeserializeIntList(part.RequiredPartIdsJson))
            {
                if (selectedIds.Contains(requiredId)) continue;
                var requiredPart = await _context.UpgradeParts
                    .AsNoTracking()
                    .Include(item => item.Category)
                    .Include(item => item.Product)
                    .FirstOrDefaultAsync(item => item.Id == requiredId && item.IsActive, cancellationToken);
                if (requiredPart is null) continue;

                result.MissingRequirements.Add(new MissingRequirement
                {
                    PartId = requiredPart.Id,
                    PartName = requiredPart.Product.Name,
                    CategoryId = requiredPart.UpgradeCategoryId,
                    CategoryName = requiredPart.Category.Name,
                    Reason = $"Required by {part.Product.Name}",
                    RequiredByPartId = part.Id,
                    RequiredByPartName = part.Product.Name
                });
                result.Errors.Add($"{part.Product.Name} requires {requiredPart.Product.Name}.");
            }

            foreach (var conflictId in DeserializeIntList(part.ConflictingPartIdsJson))
            {
                var conflictingPart = parts.FirstOrDefault(item => item.Id == conflictId);
                if (conflictingPart is null || part.Id >= conflictingPart.Id) continue;

                result.Conflicts.Add(new BuildPartConflict
                {
                    PartId = part.Id,
                    PartName = part.Product.Name,
                    ConflictingPartId = conflictingPart.Id,
                    ConflictingPartName = conflictingPart.Product.Name,
                    Reason = "These parts cannot be used together."
                });
                result.Errors.Add($"{part.Product.Name} conflicts with {conflictingPart.Product.Name}.");
            }
        }

        if (stageId.HasValue)
        {
            var stage = await _context.UpgradeStages
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Id == stageId.Value && item.IsActive && item.BikeModelId == bikeModelId,
                    cancellationToken);
            if (stage is null)
            {
                result.Errors.Add("The selected package is not available for this motorcycle.");
            }
            else
            {
                var selectedCategoryIds = parts.Select(part => part.UpgradeCategoryId).ToHashSet();
                var requiredCategoryIds = DeserializeIntList(stage.RequiredCategoryIdsJson);
                var missingCategories = await _context.UpgradeCategories
                    .AsNoTracking()
                    .Where(category =>
                        requiredCategoryIds.Contains(category.Id) &&
                        !selectedCategoryIds.Contains(category.Id))
                    .Select(category => category.Name)
                    .ToListAsync(cancellationToken);
                result.Errors.AddRange(missingCategories.Select(name =>
                    $"Package '{stage.Name}' requires a part from {name}."));
            }
        }

        result.IsValid = result.Errors.Count == 0;
        if (result.IsValid)
        {
            var selectedCategoryIds = parts.Select(part => part.UpgradeCategoryId).ToHashSet();
            var suggestions = await _context.UpgradeCategories
                .AsNoTracking()
                .Where(category =>
                    category.IsActive &&
                    category.IsRequired &&
                    !selectedCategoryIds.Contains(category.Id))
                .Select(category => category.Name)
                .ToListAsync(cancellationToken);
            result.Suggestions.AddRange(suggestions.Select(name =>
                $"Consider adding a {name} part for a complete engine setup."));
        }

        return result;
    }

    public async Task<List<UpgradePart>> GetCompatiblePartsAsync(
        int bikeModelId,
        int categoryId,
        IReadOnlyCollection<int> alreadySelected,
        CancellationToken cancellationToken = default)
    {
        var bike = await _context.BikeModels
            .AsNoTracking()
            .FirstOrDefaultAsync(model => model.Id == bikeModelId && model.IsActive, cancellationToken);
        if (bike is null) return [];

        var selectedIds = alreadySelected.ToHashSet();
        var parts = await _context.UpgradeParts
            .AsNoTracking()
            .Include(part => part.Category)
            .Include(part => part.Product)
            .Where(part =>
                part.IsActive &&
                part.Category.IsActive &&
                part.UpgradeCategoryId == categoryId &&
                !selectedIds.Contains(part.Id))
            .ToListAsync(cancellationToken);

        return parts.Where(part => IsCompatibleWithBike(part, bike)).ToList();
    }

    private static bool IsCompatibleWithBike(UpgradePart part, BikeModel bike)
    {
        if (string.IsNullOrWhiteSpace(part.CompatibleModelsJson) || part.CompatibleModelsJson == "[]")
            return true;

        var modelIds = DeserializeIntList(part.CompatibleModelsJson);
        if (modelIds.Count > 0) return modelIds.Contains(bike.Id);

        return DeserializeStringList(part.CompatibleModelsJson).Any(model =>
            bike.Model.Contains(model, StringComparison.OrdinalIgnoreCase) ||
            model.Contains(bike.Model, StringComparison.OrdinalIgnoreCase) ||
            $"{bike.Brand} {bike.Model}".Contains(model, StringComparison.OrdinalIgnoreCase));
    }

    private static List<int> DeserializeIntList(string? json)
    {
        try { return JsonSerializer.Deserialize<List<int>>(json ?? "[]") ?? []; }
        catch (JsonException) { return []; }
    }

    private static List<string> DeserializeStringList(string? json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json ?? "[]") ?? []; }
        catch (JsonException) { return []; }
    }
}
