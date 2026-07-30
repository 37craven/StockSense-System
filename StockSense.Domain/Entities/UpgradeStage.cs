using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StockSense.Domain.Entities;

public class UpgradeStage
{
    public int Id { get; set; }
    public int BikeModelId { get; set; }

    [JsonIgnore]
    public BikeModel BikeModel { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int StageNumber { get; set; }
    public int TargetCC { get; set; }
    public int EstimatedHP { get; set; }
    public int EstimatedTorque { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedCost { get; set; }

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public string RequiredCategoryIdsJson { get; set; } = "[]";
    public string RecommendedPartIdsJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public bool IsGuidedPath { get; set; } = true;
}
