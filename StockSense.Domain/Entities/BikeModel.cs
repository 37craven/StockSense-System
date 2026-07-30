using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StockSense.Domain.Entities;

public class BikeModel
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Brand { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    public int YearStart { get; set; }
    public int YearEnd { get; set; }
    public int BaseCC { get; set; }
    public int BaseHP { get; set; }
    public int BaseTorque { get; set; }

    [MaxLength(20)]
    public string EngineCode { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public ICollection<UpgradeStage> Stages { get; set; } = new List<UpgradeStage>();

    public string DisplayName => $"{Brand} {Model} ({YearStart}-{YearEnd})";
}
