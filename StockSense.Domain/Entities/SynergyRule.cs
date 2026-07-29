using System.ComponentModel.DataAnnotations;

namespace StockSense.Domain.Entities;

public class SynergyRule
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public string RequiredCategoryIdsJson { get; set; } = "[]";
    public int HPBonusPercent { get; set; }
    public int TorqueBonusPercent { get; set; }
    public int ReliabilityBonus { get; set; }
    public bool IsActive { get; set; } = true;
}
