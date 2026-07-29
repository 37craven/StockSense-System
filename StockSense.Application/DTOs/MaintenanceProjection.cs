namespace StockSense.Application.DTOs;

public class MaintenanceProjection
{
    public int OilChangeIntervalKm { get; set; }
    public string OilType { get; set; } = string.Empty;
    public int CoolantChangeIntervalKm { get; set; }
    public int BrakeFluidIntervalMonths { get; set; }
    public int ValveClearanceCheckIntervalKm { get; set; }
    public int ValveSpringReplaceIntervalKm { get; set; }
    public int PistonRingIntervalKm { get; set; }
    public int ConRodBearingIntervalKm { get; set; }
    public int MainBearingIntervalKm { get; set; }
    public string FuelRequirement { get; set; } = string.Empty;
    public int ECUTuneCheckIntervalKm { get; set; }
    public int ChainAdjustIntervalKm { get; set; }
    public int SprocketReplaceIntervalKm { get; set; }
    public int ClutchPlateIntervalKm { get; set; }
    public string MaintenanceTier { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
    public List<string> Tips { get; set; } = new();
    public double StressFactor { get; set; }
}
