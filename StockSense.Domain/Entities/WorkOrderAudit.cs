namespace StockSense.Domain.Entities;

public sealed class WorkOrderAudit
{
    public int Id { get; set; }
    public string WorkOrderType { get; set; } = string.Empty;
    public int WorkOrderId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string? ApproverUserId { get; set; }
    public string? ApproverEmail { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
