namespace StockSense.Application.DTOs;

public sealed class ValidateBuildRequest
{
    public int BikeModelId { get; set; }
    public List<int> PartIds { get; set; } = new();
    public int? StageId { get; set; }
}

public sealed class CalculateBuildRequest
{
    public int BikeModelId { get; set; }
    public List<int> PartIds { get; set; } = new();
}

public sealed class CalculateStageRequest
{
    public int BikeModelId { get; set; }
    public int StageId { get; set; }
    public List<int>? CustomPartIds { get; set; }
}

public sealed class MaintenanceRequest
{
    public int BikeModelId { get; set; }
    public List<int> PartIds { get; set; } = new();
}

public sealed class SubmitEngineBuildRequest
{
    public int DraftId { get; set; }
    public string? BuildName { get; set; }
}

public sealed record BuildCustomerIdentity(
    string UserId,
    string? Email,
    string DisplayName);
