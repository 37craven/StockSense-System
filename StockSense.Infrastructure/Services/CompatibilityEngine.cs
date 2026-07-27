using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StockSense.Infrastructure.Data;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Services
{
    public interface ICompatibilityEngine
    {
        Task<ValidationResult> ValidateBuildAsync(int bikeModelId, List<int> partIds, int? stageId = null);
        Task<List<UpgradePart>> GetCompatiblePartsAsync(int bikeModelId, int categoryId, List<int> alreadySelected);
        Task<RequirementCheck> CheckRequirementsAsync(int bikeModelId, List<int> selectedPartIds);
        Task<List<Conflict>> DetectConflictsAsync(List<int> selectedPartIds);
    }

    public class RequirementCheck
    {
        public bool AllSatisfied { get; set; }
        public List<MissingRequirement> Missing { get; set; } = new();
    }

    public class CompatibilityEngine : ICompatibilityEngine
    {
        private readonly ApplicationDbContext _context;

        public CompatibilityEngine(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ValidationResult> ValidateBuildAsync(int bikeModelId, List<int> partIds, int? stageId = null)
        {
            var result = new ValidationResult();
            var parts = await _context.UpgradeParts
                .Include(p => p.Category)
                .Include(p => p.Product)
                .Where(p => partIds.Contains(p.Id) && p.IsActive)
                .ToListAsync();

            if (parts.Count != partIds.Count)
            {
                var foundIds = parts.Select(p => p.Id).ToHashSet();
                var missing = partIds.Where(id => !foundIds.Contains(id)).ToList();
                result.Errors.Add($"Parts not found or inactive: {string.Join(", ", missing)}");
            }

            // Check bike compatibility
            var bike = await _context.BikeModels.FindAsync(bikeModelId);
            if (bike == null)
            {
                result.Errors.Add("Invalid bike model selected.");
                return result;
            }

            foreach (var part in parts)
            {
                if (!IsCompatibleWithBike(part, bike))
                {
                    result.Errors.Add($"{part.Product?.Name ?? "Unknown part"} is not compatible with {bike.DisplayName}");
                }
            }

            // Check requirements
            var reqCheck = await CheckRequirementsAsync(bikeModelId, partIds);
            result.MissingRequirements = reqCheck.Missing;

            // Check conflicts
            result.Conflicts = await DetectConflictsAsync(partIds);

            // Stage validation
            if (stageId.HasValue)
            {
                var stage = await _context.UpgradeStages.FindAsync(stageId.Value);
                if (stage != null)
                {
                    var requiredCats = DeserializeIntList(stage.RequiredCategoryIdsJson);
                    var selectedCatIds = parts.Select(p => p.UpgradeCategoryId).Distinct().ToHashSet();

                    foreach (var reqCatId in requiredCats)
                    {
                        if (!selectedCatIds.Contains(reqCatId))
                        {
                            var cat = await _context.UpgradeCategories.FindAsync(reqCatId);
                            result.Errors.Add($"Stage '{stage.Name}' requires a part from category: {cat?.Name ?? "Unknown"}");
                        }
                    }

                    // Check if parts are typically used for this stage
                    foreach (var part in parts)
                    {
                        var requiredForStages = DeserializeStringList(part.RequiredForStagesJson);
                        if (!requiredForStages.Contains(stage.Name))
                        {
                            result.Warnings.Add($"{part.Product?.Name} is not typically used in Stage '{stage.Name}'");
                        }
                    }
                }
            }

            // Suggestions
            if (result.IsValid)
            {
                var selectedCatIds = parts.Select(p => p.UpgradeCategoryId).Distinct().ToHashSet();
                var allCats = await _context.UpgradeCategories.Where(c => c.IsActive).ToListAsync();

                foreach (var cat in allCats.Where(c => c.IsRequired && !selectedCatIds.Contains(c.Id)))
                {
                    result.Suggestions.Add($"Consider adding a {cat.Name} for a complete build");
                }
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }

        public async Task<List<UpgradePart>> GetCompatiblePartsAsync(int bikeModelId, int categoryId, List<int> alreadySelected)
        {
            var bike = await _context.BikeModels.FindAsync(bikeModelId);
            if (bike == null) return new List<UpgradePart>();

            var parts = await _context.UpgradeParts
                .Include(p => p.Category)
                .Include(p => p.Product)
                .Where(p => p.IsActive && p.Product != null && p.UpgradeCategoryId == categoryId)
                .ToListAsync();

            return parts
                .Where(p => IsCompatibleWithBike(p, bike))
                .Where(p => !alreadySelected.Contains(p.Id))
                .ToList();
        }

        public async Task<RequirementCheck> CheckRequirementsAsync(int bikeModelId, List<int> selectedPartIds)
        {
            var check = new RequirementCheck();
            var parts = await _context.UpgradeParts
                .Include(p => p.Category)
                .Include(p => p.Product)
                .Where(p => selectedPartIds.Contains(p.Id))
                .ToListAsync();

            var selectedIds = selectedPartIds.ToHashSet();
            var missing = new List<MissingRequirement>();

            foreach (var part in parts)
            {
                var requiredIds = DeserializeIntList(part.RequiredPartIdsJson);

                foreach (var reqId in requiredIds)
                {
                    if (!selectedIds.Contains(reqId))
                    {
                        var reqPart = await _context.UpgradeParts
                            .Include(p => p.Category)
                            .Include(p => p.Product)
                            .FirstOrDefaultAsync(p => p.Id == reqId);

                        if (reqPart != null)
                        {
                            missing.Add(new MissingRequirement
                            {
                                PartId = reqPart.Id,
                                PartName = reqPart.Product?.Name ?? "Unknown",
                                CategoryId = reqPart.UpgradeCategoryId,
                                CategoryName = reqPart.Category?.Name ?? "Unknown",
                                Reason = $"Required by {part.Product?.Name ?? "selected part"}",
                                RequiredByPartId = part.Id,
                                RequiredByPartName = part.Product?.Name ?? "Unknown"
                            });
                        }
                    }
                }
            }

            check.Missing = missing;
            check.AllSatisfied = !missing.Any();
            return check;
        }

        public async Task<List<Conflict>> DetectConflictsAsync(List<int> selectedPartIds)
        {
            var conflicts = new List<Conflict>();
            var parts = await _context.UpgradeParts
                .Include(p => p.Product)
                .Where(p => selectedPartIds.Contains(p.Id))
                .ToListAsync();

            var selectedIds = selectedPartIds.ToHashSet();

            foreach (var part in parts)
            {
                var conflictIds = DeserializeIntList(part.ConflictingPartIdsJson);

                foreach (var conflictId in conflictIds)
                {
                    if (selectedIds.Contains(conflictId))
                    {
                        var conflictPart = parts.FirstOrDefault(p => p.Id == conflictId);
                        if (conflictPart != null)
                        {
                            conflicts.Add(new Conflict
                            {
                                PartId = part.Id,
                                PartName = part.Product?.Name ?? "Unknown",
                                ConflictingPartId = conflictPart.Id,
                                ConflictingPartName = conflictPart.Product?.Name ?? "Unknown",
                                Reason = "These parts cannot be used together"
                            });
                        }
                    }
                }
            }

            return conflicts;
        }

        private bool IsCompatibleWithBike(UpgradePart part, BikeModel bike)
        {
            if (string.IsNullOrEmpty(part.CompatibleModelsJson) || part.CompatibleModelsJson == "[]")
                return true; // Universal part

            var modelIds = DeserializeIntList(part.CompatibleModelsJson);
            if (modelIds.Count > 0)
            {
                return modelIds.Contains(bike.Id);
            }

            var models = DeserializeStringList(part.CompatibleModelsJson);
            return models.Any(m =>
                bike.Model.Contains(m, StringComparison.OrdinalIgnoreCase) ||
                m.Contains(bike.Model, StringComparison.OrdinalIgnoreCase) ||
                (bike.Brand + " " + bike.Model).Contains(m, StringComparison.OrdinalIgnoreCase)
            );
        }

        private static List<int> DeserializeIntList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json) || json == "[]")
                return new List<int>();

            try
            {
                return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
            }
            catch (JsonException)
            {
                return new List<int>();
            }
        }

        private static List<string> DeserializeStringList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json) || json == "[]")
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }
    }
}
