using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StockSense.Domain.Entities;

public class CustomerBuild
{
    public int Id { get; set; }

    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public int? BikeModelId { get; set; }

    [JsonIgnore]
    public BikeModel? BikeModel { get; set; }

    public int? UpgradeStageId { get; set; }

    [JsonIgnore]
    public UpgradeStage? UpgradeStage { get; set; }

    public int? BuildRequestId { get; set; }

    [JsonIgnore]
    public BuildRequest? BuildRequest { get; set; }

    public string SelectedPartIdsJson { get; set; } = "[]";
    public int CurrentCC { get; set; }
    public int ProjectedHP { get; set; }
    public int ProjectedTorque { get; set; }
    public int ReliabilityScore { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPartsCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedLaborCost { get; set; }

    [NotMapped]
    public decimal TotalCost => TotalPartsCost + EstimatedLaborCost;

    [MaxLength(20)]
    public string Status { get; set; } = EngineBuildStatuses.Draft;

    public string ValidationWarningsJson { get; set; } = "[]";
    public string ValidationErrorsJson { get; set; } = "[]";
    public string MissingRequirementsJson { get; set; } = "[]";
    public string MaintenanceProjectionJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class EngineBuildStatuses
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
}
