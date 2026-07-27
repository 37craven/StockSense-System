using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StockSense.Domain.Entities
{
    public class UpgradeCategory
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Icon { get; set; } = string.Empty; // Lucide icon name

        public int DisplayOrder { get; set; }

        public bool IsRequired { get; set; }

        public bool AllowsMultiple { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string CompatibilityNotes { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Runtime property for API response (not persisted)
        [NotMapped]
        [JsonIgnore]
        public int PartCount { get; set; }

        // Navigation
        [JsonIgnore]
        public virtual ICollection<UpgradePart> UpgradeParts { get; set; } = new List<UpgradePart>();
    }
}
