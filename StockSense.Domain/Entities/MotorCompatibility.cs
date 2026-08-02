namespace StockSense.Domain.Entities;

public class MotorCompatibility
{
    public int CompatibilityId { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string VersionName { get; set; } = string.Empty;
    public int YearStart { get; set; }
    public int? YearEnd { get; set; }
    public string? EngineOilSpec { get; set; }
    public string? GearOilSpec { get; set; }
    public string? CoolantSpec { get; set; }
    public string? SparkPlugSpec { get; set; }
    public string? FuelFilterSpec { get; set; }
    public string? DriveBeltSpec { get; set; }
    public string? FlyBallWeight { get; set; }
    public string? CenterSpringSpec { get; set; }
    public string? BrakePadFront { get; set; }
    public string? BrakePadRear { get; set; }
    public string? BrakeShoeRear { get; set; }
    public string? AirFilterSpec { get; set; }

    public ICollection<ProductCompatibilityMapping> ProductMappings { get; set; } =
        new List<ProductCompatibilityMapping>();
}
