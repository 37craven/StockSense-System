using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StockSense.Domain.Entities
{
    public class SynergyRule
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        // JSON array of required category IDs that must all be present
        public string RequiredCategoryIdsJson { get; set; } = "[]";

        // Bonus percentages when all required categories are selected
        public int HPBonusPercent { get; set; }

        public int TorqueBonusPercent { get; set; }

        // Optional: reliability bonus (reduces stress)
        public int ReliabilityBonus { get; set; }

        public bool IsActive { get; set; } = true;
    }
}