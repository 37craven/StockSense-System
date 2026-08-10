namespace StockSense.Application.DTOs;

public sealed class UpdateWorkOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? AdminUserId { get; set; }
    public string? AdminEmail { get; set; }
    public string? AdminPin { get; set; }
}

public class AdminOverrideDto
{
    public string? AdminUserId { get; set; }
    public string? AdminEmail { get; set; }
    public string? AdminPin { get; set; }
    public string? Reason { get; set; }
}

public sealed class UpdateBuildPartsDto : AdminOverrideDto
{
    public List<int> ProductIds { get; set; } = [];
}

public sealed class SetAdminPinDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPin { get; set; } = string.Empty;
    public string ConfirmPin { get; set; } = string.Empty;
}
