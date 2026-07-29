using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockSense.Domain.Entities;

public class UpgradePart
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int UpgradeCategoryId { get; set; }
    public UpgradeCategory Category { get; set; } = null!;

    public int CCGain { get; set; }
    public int HPGain { get; set; }
    public int TorqueGain { get; set; }
    public int ReliabilityImpact { get; set; }

    [MaxLength(500)]
    public string RenderImageUrl { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ListPrice { get; set; }

    [Column(TypeName = "decimal(4,2)")]
    public decimal EstimatedLaborHours { get; set; }

    public string CompatibleModelsJson { get; set; } = "[]";
    public string RequiredPartIdsJson { get; set; } = "[]";
    public string ConflictingPartIdsJson { get; set; } = "[]";
    public string RequiredForStagesJson { get; set; } = "[]";

    public double CompressionRatioImpact { get; set; }
    public int RedlineRPMChange { get; set; }
    public double BottomEndStressMultiplier { get; set; } = 1.0;
    public double ValvetrainStressMultiplier { get; set; } = 1.0;
    public bool RequiresRaceFuel { get; set; }
    public bool BreakInRequired { get; set; }

    [MaxLength(500)]
    public string BreakInNotes { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string InstallNotes { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [MaxLength(50)]
    public string PresetTemplate { get; set; } = string.Empty;
}
