namespace StockSense.Domain.Entities;

public static class WorkOrderRules
{
    public static string? ValidateStatusTransition(string currentStatus, string targetStatus)
    {
        var isAllowed = (currentStatus, targetStatus) switch
        {
            (WorkOrderStatuses.Pending, WorkOrderStatuses.Confirmed) => true,
            (WorkOrderStatuses.Pending, WorkOrderStatuses.Cancelled) => true,
            (WorkOrderStatuses.Confirmed, WorkOrderStatuses.Cancelled) => true,
            _ => false
        };

        return isAllowed
            ? null
            : $"A work order cannot transition from {currentStatus} to {targetStatus}.";
    }
}
