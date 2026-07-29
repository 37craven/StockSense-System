namespace StockSense.Client.Components.BuildEngine;

public sealed class EngineBikeModel
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int YearStart { get; set; }
    public int YearEnd { get; set; }
    public int BaseCC { get; set; }
    public int BaseHP { get; set; }
    public int BaseTorque { get; set; }
    public string EngineCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public string DisplayName => $"{Brand} {Model} ({YearStart}-{YearEnd})";
}

public sealed class EngineUpgradeCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool AllowsMultiple { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EngineProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CurrentStock { get; set; }
    public int ReorderTarget { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Barcode { get; set; }
}

public sealed class EngineUpgradePart
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public EngineProduct? Product { get; set; }
    public int UpgradeCategoryId { get; set; }
    public EngineUpgradeCategory? Category { get; set; }
    public int CCGain { get; set; }
    public int HPGain { get; set; }
    public int TorqueGain { get; set; }
    public int ReliabilityImpact { get; set; }
    public decimal ListPrice { get; set; }
    public decimal EstimatedLaborHours { get; set; }
    public string CompatibleModelsJson { get; set; } = "[]";
    public string ConflictingPartIdsJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;

    public decimal EffectivePrice => ListPrice > 0 ? ListPrice : Product?.Price ?? 0;
}

public sealed class EngineBuildProjection
{
    public int BikeModelId { get; set; }
    public int BaseCC { get; set; }
    public int BaseHP { get; set; }
    public int BaseTorque { get; set; }
    public int FinalCC { get; set; }
    public int FinalHP { get; set; }
    public int FinalTorque { get; set; }
    public int ReliabilityScore { get; set; } = 100;
    public decimal TotalPartsCost { get; set; }
    public decimal EstimatedLaborCost { get; set; }
    public decimal TotalCost { get; set; }
    public string MatchedStageName { get; set; } = string.Empty;
    public int? MatchedStageNumber { get; set; }
    public EngineMaintenanceProjection? Maintenance { get; set; }
    public List<string> ValidationErrors { get; set; } = [];
    public List<string> ValidationWarnings { get; set; } = [];
    public List<string> ValidationSuggestions { get; set; } = [];
}

public sealed class EngineMaintenanceProjection
{
    public int OilChangeIntervalKm { get; set; }
    public string OilType { get; set; } = string.Empty;
    public int ValveClearanceCheckIntervalKm { get; set; }
    public int ValveSpringReplaceIntervalKm { get; set; }
    public int PistonRingIntervalKm { get; set; }
    public int ConRodBearingIntervalKm { get; set; }
    public int MainBearingIntervalKm { get; set; }
    public string FuelRequirement { get; set; } = string.Empty;
    public int ChainAdjustIntervalKm { get; set; }
    public int SprocketReplaceIntervalKm { get; set; }
    public int ClutchPlateIntervalKm { get; set; }
    public string MaintenanceTier { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = [];
    public List<string> Tips { get; set; } = [];
}
