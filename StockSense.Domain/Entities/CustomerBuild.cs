using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StockSense.Domain.Entities
{
    public class CustomerBuild
    {
        public int Id { get; set; }

        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        public int? BikeModelId { get; set; }

        [JsonIgnore]
        public virtual BikeModel? BikeModel { get; set; }

        public int? UpgradeStageId { get; set; }

        [JsonIgnore]
        public virtual UpgradeStage? UpgradeStage { get; set; }

        // JSON array of selected UpgradePart IDs
        public string SelectedPartIdsJson { get; set; } = "[]";

        // Computed projection fields (cached for quick display)
        public int CurrentCC { get; set; }

        public int ProjectedHP { get; set; }

        public int ProjectedTorque { get; set; }

        public int ReliabilityScore { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPartsCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedLaborCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost => TotalPartsCost + EstimatedLaborCost;

        // Build status
        [MaxLength(20)]
        public string Status { get; set; } = "Draft"; // Draft, Submitted, Quoted, Approved, Ordered

        // Validation results (cached)
        public string ValidationWarningsJson { get; set; } = "[]";

        public string ValidationErrorsJson { get; set; } = "[]";

        public string MissingRequirementsJson { get; set; } = "[]";

        // Maintenance projection (cached)
        public string MaintenanceProjectionJson { get; set; } = "{}";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
