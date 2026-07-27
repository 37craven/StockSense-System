using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StockSense.Infrastructure.Data;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Services
{
    public interface IPerformanceCalculator
    {
        Task<BuildProjection> CalculateAsync(int bikeModelId, List<int> partIds);
        Task<BuildProjection> CalculateForStageAsync(int bikeModelId, int stageId, List<int>? customPartIds = null);
        Task<MaintenanceProjection> CalculateMaintenanceAsync(int bikeModelId, List<int> partIds);
    }

    public class PerformanceCalculator : IPerformanceCalculator
    {
        private readonly ApplicationDbContext _context;
        private readonly ICompatibilityEngine _compatibility;

        public PerformanceCalculator(ApplicationDbContext context, ICompatibilityEngine compatibility)
        {
            _context = context;
            _compatibility = compatibility;
        }

        public async Task<BuildProjection> CalculateAsync(int bikeModelId, List<int> partIds)
        {
            var bike = await _context.BikeModels.FindAsync(bikeModelId);
            if (bike == null)
                throw new ArgumentException("Bike model not found");

            var parts = await _context.UpgradeParts
                .Include(p => p.Category)
                .Include(p => p.Product)
                .Where(p => partIds.Contains(p.Id) && p.IsActive)
                .ToListAsync();

            var validation = await _compatibility.ValidateBuildAsync(bikeModelId, partIds);
            var maintenance = await CalculateMaintenanceAsync(bikeModelId, partIds);

            return ComputeProjection(bike, parts, maintenance, validation);
        }

        public async Task<BuildProjection> CalculateForStageAsync(int bikeModelId, int stageId, List<int>? customPartIds = null)
        {
            var bike = await _context.BikeModels.FindAsync(bikeModelId);
            if (bike == null)
                throw new ArgumentException("Bike model not found");

            var stage = await _context.UpgradeStages.FindAsync(stageId);
            if (stage == null)
                throw new ArgumentException("Stage not found");

            List<int> partIds = customPartIds ?? new List<int>();

            // If no custom parts, get recommended parts for this stage
            if (!customPartIds?.Any() ?? true)
            {
                var recIds = JsonSerializer.Deserialize<List<int>>(stage.RecommendedPartIdsJson) ?? new List<int>();
                partIds = recIds;
            }

            var parts = await _context.UpgradeParts
                .Include(p => p.Category)
                .Include(p => p.Product)
                .Where(p => partIds.Contains(p.Id) && p.IsActive)
                .ToListAsync();

            var maintenance = await CalculateMaintenanceAsync(bikeModelId, partIds);
            return ComputeProjection(bike, parts, maintenance, null);
        }

        public async Task<MaintenanceProjection> CalculateMaintenanceAsync(int bikeModelId, List<int> partIds)
        {
            var bike = await _context.BikeModels.FindAsync(bikeModelId);
            if (bike == null)
                throw new ArgumentException("Bike model not found");

            var parts = await _context.UpgradeParts
                .Include(p => p.Category)
                .Include(p => p.Product)
                .Where(p => partIds.Contains(p.Id) && p.IsActive)
                .ToListAsync();

            // Compute a temporary projection for maintenance calc
            var tempProjection = new BuildProjection
            {
                BaseCC = bike.BaseCC,
                BaseHP = bike.BaseHP,
                BaseTorque = bike.BaseTorque,
                AddedCC = parts.Sum(p => p.CCGain),
                AddedHP = parts.Sum(p => p.HPGain),
                AddedTorque = parts.Sum(p => p.TorqueGain)
            };
            tempProjection.FinalCC = tempProjection.BaseCC + tempProjection.AddedCC;
            tempProjection.FinalHP = tempProjection.BaseHP + tempProjection.AddedHP;
            tempProjection.FinalTorque = tempProjection.BaseTorque + tempProjection.AddedTorque;

            return ComputeMaintenance(bike, parts, tempProjection);
        }

        private BuildProjection ComputeProjection(BikeModel bike, List<UpgradePart> parts, MaintenanceProjection maintenance, ValidationResult? validation)
        {
            var projection = new BuildProjection
            {
                BaseCC = bike.BaseCC,
                BaseHP = bike.BaseHP,
                BaseTorque = bike.BaseTorque,
                BikeModelId = bike.Id,
                BikeName = bike.DisplayName
            };

            // Sum up gains
            projection.AddedCC = parts.Sum(p => p.CCGain);
            projection.AddedHP = parts.Sum(p => p.HPGain);
            projection.AddedTorque = parts.Sum(p => p.TorqueGain);
            projection.ReliabilityScore = 100 + parts.Sum(p => p.ReliabilityImpact);

            // Apply synergy bonuses
            var synergy = ApplySynergyBonuses(parts);
            projection.AddedHP += synergy.HPBonus;
            projection.AddedTorque += synergy.TorqueBonus;
            projection.ReliabilityScore += synergy.ReliabilityBonus;

            projection.FinalCC = projection.BaseCC + projection.AddedCC;
            projection.FinalHP = projection.BaseHP + projection.AddedHP;
            projection.FinalTorque = projection.BaseTorque + projection.AddedTorque;

            // Costs
            projection.TotalPartsCost = parts.Sum(p => p.ListPrice > 0 ? p.ListPrice : p.Product?.Price ?? 0);
            projection.EstimatedLaborCost = parts.Sum(p => p.EstimatedLaborHours * 500m); // ₱500/hour labor rate
            projection.TotalCost = projection.TotalPartsCost + projection.EstimatedLaborCost;

            // Stage info
            var matchedStage = GetMatchedStage(bike.Id, projection);
            if (matchedStage != null)
            {
                projection.MatchedStageName = matchedStage.Name;
                projection.MatchedStageNumber = matchedStage.StageNumber;
            }

            // Maintenance
            projection.Maintenance = maintenance;

            // Validation
            if (validation != null)
            {
                projection.ValidationErrors = validation.Errors;
                projection.ValidationWarnings = validation.Warnings;
                projection.ValidationSuggestions = validation.Suggestions;
            }

            projection.IsValid = projection.FinalCC > 0;
            return projection;
        }

        private MaintenanceProjection ComputeMaintenance(BikeModel bike, List<UpgradePart> parts, BuildProjection projection)
        {
            var proj = new MaintenanceProjection
            {
                // Stock defaults (conservative 150cc scooter)
                OilChangeIntervalKm = 3000,
                OilType = "Full Synthetic 10W-40",
                CoolantChangeIntervalKm = 20000,
                BrakeFluidIntervalMonths = 12,
                ValveClearanceCheckIntervalKm = 10000,
                ValveSpringReplaceIntervalKm = 30000,
                PistonRingIntervalKm = 40000,
                ConRodBearingIntervalKm = 60000,
                MainBearingIntervalKm = 80000,
                FuelRequirement = "RON 91+",
                ECUTuneCheckIntervalKm = 10000,
                ChainAdjustIntervalKm = 500,
                SprocketReplaceIntervalKm = 15000,
                ClutchPlateIntervalKm = 20000
            };

            // Calculate stress factor
            double stressRatio = bike.BaseCC > 0 ? (double)parts.Sum(p => p.CCGain) / bike.BaseCC : 0;
            double rpmFactor = parts.Any(p => p.Category?.Name.Contains("Crankshaft") == true) ? 1.2 : 1.0;
            double compFactor = parts.Any(p => p.RequiresRaceFuel) ? 1.3 : 1.0;
            double valvetrainFactor = parts.Any(p => p.Category?.Name.Contains("Valve") == true || p.Category?.Name.Contains("Camshaft") == true) ? 1.5 : 1.0;
            double forcedInduction = parts.Any(p => p.Category?.Name.Contains("Turbo") == true || p.Category?.Name.Contains("Supercharger") == true) ? 1.5 : 1.0;

            var hpGainRatio = bike.BaseHP > 0 ? (double)parts.Sum(p => p.HPGain) / bike.BaseHP : 0;
            var systemComplexity = parts.Count * 0.05;
            var reliabilityPenalty = Math.Abs(Math.Min(parts.Sum(p => p.ReliabilityImpact), 0)) / 100.0;
            double totalStress = (stressRatio * 1.35) + (hpGainRatio * 0.75) + systemComplexity + reliabilityPenalty;
            totalStress *= rpmFactor * compFactor * valvetrainFactor * forcedInduction;

            // Oil interval - most sensitive
            if (totalStress > 1.5)
            {
                proj.OilChangeIntervalKm = 500;
                proj.OilType = "Full Synthetic 10W-60 Racing (Motul 300V / Estoril)";
                proj.Warnings.Add("⚠️ Extreme build: Change oil every 500km with racing oil");
            }
            else if (totalStress > 1.0)
            {
                proj.OilChangeIntervalKm = 1000;
                proj.OilType = "Full Synthetic 10W-50 Racing";
                proj.Warnings.Add("⚠️ High-stress build: Oil every 1,000km with racing oil");
            }
            else if (totalStress > 0.5)
            {
                proj.OilChangeIntervalKm = 2000;
                proj.OilType = "Full Synthetic 10W-50";
            }
            else if (totalStress > 0.2)
            {
                proj.OilChangeIntervalKm = 3000;
                proj.OilType = "Full Synthetic 10W-40";
            }
            else if (parts.Any())
            {
                proj.OilChangeIntervalKm = 2500;
                proj.OilType = "Full Synthetic 10W-40";
            }

            // Fuel requirement
            double compRatio = 10.5; // stock
            compRatio += parts.Sum(p => p.CompressionRatioImpact);

            if (compRatio >= 13.0)
            {
                proj.FuelRequirement = "RON 100+ (Race Fuel / AVGAS blend)";
                proj.Warnings.Add($"⚠️ {compRatio:F1}:1 compression requires race fuel — pump gas will detonate");
            }
            else if (compRatio >= 12.0)
            {
                proj.FuelRequirement = "RON 98+ (V-Power / Blaze / Petron Blaze)";
            }
            else if (compRatio >= 11.5)
            {
                proj.FuelRequirement = "RON 95+ (Premium)";
            }
            else
            {
                proj.FuelRequirement = "RON 91+ (Regular)";
            }

            // Valvetrain
            if (valvetrainFactor > 1.0)
            {
                proj.ValveClearanceCheckIntervalKm = totalStress > 1.0 ? 3000 : 5000;
                proj.ValveSpringReplaceIntervalKm = totalStress > 1.0 ? 15000 : 30000;
                proj.Warnings.Add("⚠️ Aggressive cam/profile: Check valve clearance every " + proj.ValveClearanceCheckIntervalKm + "km");
            }

            // Bottom end
            if (parts.Any(p => p.Category?.Name.Contains("Crankshaft") == true && p.Product?.Name.Contains("Stroker") == true))
            {
                proj.ConRodBearingIntervalKm = 15000;
                proj.MainBearingIntervalKm = 20000;
                proj.Warnings.Add("⚠️ Stroker crank: Rod/main bearings every 15-20k km");
            }

            if (parts.Any(p => p.Category?.Name.Contains("Block") == true || p.Category?.Name.Contains("Piston") == true))
            {
                proj.PistonRingIntervalKm = totalStress > 1.0 ? 10000 : 20000;
                proj.Warnings.Add("⚠️ Big bore/piston: Ring replacement every " + proj.PistonRingIntervalKm + "km");
            }

            // Clutch
            if (projection.FinalTorque > 25)
            {
                proj.ClutchPlateIntervalKm = 10000;
                proj.Tips.Add("💡 Upgrade to billet clutch basket + heavy-duty springs for durability");
            }

            if (parts.Any(p => p.Category?.Name.Contains("CVT", StringComparison.OrdinalIgnoreCase) == true ||
                               p.Product?.Name.Contains("Pulley", StringComparison.OrdinalIgnoreCase) == true ||
                               p.Product?.Name.Contains("Clutch", StringComparison.OrdinalIgnoreCase) == true))
            {
                proj.ClutchPlateIntervalKm = Math.Min(proj.ClutchPlateIntervalKm, 15000);
                proj.Tips.Add("CVT setup selected: inspect belt, pulley face, rollers, and clutch shoe condition during PMS.");
            }

            if (parts.Any(p => p.Category?.Name.Contains("Exhaust", StringComparison.OrdinalIgnoreCase) == true))
            {
                proj.ECUTuneCheckIntervalKm = 5000;
                proj.Tips.Add("Exhaust selected: check exhaust leaks and confirm air-fuel tuning after installation.");
            }

            // Tier classification
            if (totalStress <= 0.3)
                proj.MaintenanceTier = "Street";
            else if (totalStress <= 0.6)
                proj.MaintenanceTier = "Sport";
            else if (totalStress <= 1.0)
                proj.MaintenanceTier = "Race";
            else
                proj.MaintenanceTier = "Drag/Extreme";

            // Break-in
            if (parts.Any(p => p.BreakInRequired))
            {
                proj.Tips.Add("🔧 BREAK-IN: First 300km — vary RPM, no WOT, oil change at 100km & 300km, retorque head at 300km");
                proj.Tips.AddRange(parts.Where(p => p.BreakInRequired).Select(p => p.BreakInNotes));
            }

            // Stress factor for reference
            proj.StressFactor = Math.Round(totalStress, 2);

            return proj;
        }

        private (int HPBonus, int TorqueBonus, int ReliabilityBonus) ApplySynergyBonuses(List<UpgradePart> parts)
        {
            int hpBonus = 0, torqueBonus = 0, relBonus = 0;
            var selectedCatIds = parts.Select(p => p.UpgradeCategoryId).Distinct().ToHashSet();

            // TODO: Query active SynergyRules from DB and apply dynamically
            // For now, hardcoded common synergies

            // Big Bore Kit (Block + Piston) - handled by single part usually
            // Stroker Build (Crank + Block)
            if (selectedCatIds.Contains(1) && selectedCatIds.Contains(2)) // Block + Crank
            {
                hpBonus += 12;
                torqueBonus += 15;
                relBonus -= 5;
            }

            // Top End Package (Head + Valves + Cam)
            if (selectedCatIds.Contains(3) && selectedCatIds.Contains(4) && selectedCatIds.Contains(5))
            {
                hpBonus += 18;
                torqueBonus += 10;
                relBonus -= 10;
            }

            // Breathing Package (TB + Exhaust + ECU)
            if (selectedCatIds.Contains(6) && selectedCatIds.Contains(8) && selectedCatIds.Contains(7))
            {
                hpBonus += 10;
                torqueBonus += 8;
            }

            // Full Race Build
            if (selectedCatIds.Count >= 7)
            {
                hpBonus += 15;
                torqueBonus += 10;
                relBonus -= 15;
            }

            return (hpBonus, torqueBonus, relBonus);
        }

        private UpgradeStage? GetMatchedStage(int bikeModelId, BuildProjection proj)
        {
            // Find stage that matches the final CC closely
            var stages = _context.UpgradeStages
                .Where(s => s.BikeModelId == bikeModelId && s.IsActive)
                .ToList();

            return stages
                .OrderBy(s => Math.Abs(s.TargetCC - proj.FinalCC))
                .FirstOrDefault();
        }
    }
}
