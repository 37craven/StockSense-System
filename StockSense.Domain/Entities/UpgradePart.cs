using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockSense.Domain.Entities
{
    public class UpgradePart
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public virtual Product Product { get; set; } = null!;

        public int UpgradeCategoryId { get; set; }

        public virtual UpgradeCategory Category { get; set; } = null!;

        // Performance Specs
        public int CCGain { get; set; }

        public int HPGain { get; set; }

        public int TorqueGain { get; set; }

        // Reliability impact: -20 to +10 (negative = less reliable)
        public int ReliabilityImpact { get; set; }

        // Dedicated render image for build preview (fallback to Product.ImageUrl)
        [MaxLength(500)]
        public string RenderImageUrl { get; set; } = string.Empty;

        // Build quote price (separate from retail Product.Price)
        [Column(TypeName = "decimal(18,2)")]
        public decimal ListPrice { get; set; }

        // Per-part labor hours (admin configurable)
        [Column(TypeName = "decimal(4,2)")]
        public decimal EstimatedLaborHours { get; set; }

        // Compatibility
        // JSON array of compatible model names: ["Aerox 155", "NMAX 155"]
        public string CompatibleModelsJson { get; set; } = "[]";

        // JSON array of required part IDs (must also buy these)
        public string RequiredPartIdsJson { get; set; } = "[]";

        // JSON array of conflicting part IDs (cannot combine)
        public string ConflictingPartIdsJson { get; set; } = "[]";

        // JSON array of stage names this part is required for: ["Stage 2", "Stage 3"]
        public string RequiredForStagesJson { get; set; } = "[]";

        // Stress multipliers for maintenance projection
        public double CompressionRatioImpact { get; set; } = 0; // Adds to base 10.5:1

        public int RedlineRPMChange { get; set; } = 0;

        public double BottomEndStressMultiplier { get; set; } = 1.0;

        public double ValvetrainStressMultiplier { get; set; } = 1.0;

        public bool RequiresRaceFuel { get; set; } = false;

        public bool BreakInRequired { get; set; } = false;

        [MaxLength(500)]
        public string BreakInNotes { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string InstallNotes { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Preset template used (for admin reference)
        [MaxLength(50)]
        public string PresetTemplate { get; set; } = string.Empty;
    }
}