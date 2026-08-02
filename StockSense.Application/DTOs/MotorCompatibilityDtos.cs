namespace StockSense.Application.DTOs;

public sealed record MotorCompatibilityLookupQuery(
    string Manufacturer,
    string ModelName,
    string VersionName,
    int Year);

public sealed record CompatibleProductDto(
    int ProductId,
    string Name,
    string Category,
    string Brand,
    decimal Price,
    int CurrentStock,
    int ReorderTarget,
    string StockStatus,
    string ImageUrl,
    string PartFunction,
    bool IsOem,
    string? Notes);

public sealed record MotorCompatibilityDto(
    int CompatibilityId,
    string Manufacturer,
    string ModelName,
    string VersionName,
    int YearStart,
    int? YearEnd,
    string? EngineOilSpec,
    string? GearOilSpec,
    string? CoolantSpec,
    string? SparkPlugSpec,
    string? FuelFilterSpec,
    string? DriveBeltSpec,
    string? FlyBallWeight,
    string? CenterSpringSpec,
    string? BrakePadFront,
    string? BrakePadRear,
    string? BrakeShoeRear,
    string? AirFilterSpec,
    IReadOnlyList<CompatibleProductDto> Products);
