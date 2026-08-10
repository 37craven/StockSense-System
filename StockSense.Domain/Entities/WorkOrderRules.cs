namespace StockSense.Domain.Entities;

public static class WorkOrderRules
{
    public static string? ValidateStatusTransition(string currentStatus, string targetStatus, bool isAdmin)
    {
        var transitionError = ValidateStatusTransition(currentStatus, targetStatus);
        if (transitionError is not null) return transitionError;

        if (!isAdmin && currentStatus != WorkOrderStatuses.Pending)
            return "Only an admin can change a confirmed, completed, or cancelled work order.";

        return null;
    }

    public static bool RequiresAdminReason(string currentStatus, string targetStatus) =>
        currentStatus is WorkOrderStatuses.Confirmed or WorkOrderStatuses.Completed or WorkOrderStatuses.Cancelled
        && targetStatus != WorkOrderStatuses.Completed;

    public static string? ValidateStatusTransition(string currentStatus, string targetStatus)
    {
        var isAllowed = (currentStatus, targetStatus) switch
        {
            (WorkOrderStatuses.Pending, WorkOrderStatuses.Confirmed) => true,
            (WorkOrderStatuses.Pending, WorkOrderStatuses.Cancelled) => true,
            (WorkOrderStatuses.Confirmed, WorkOrderStatuses.Pending) => true,
            (WorkOrderStatuses.Confirmed, WorkOrderStatuses.Cancelled) => true,
            (WorkOrderStatuses.Completed, WorkOrderStatuses.Pending) => true,
            (WorkOrderStatuses.Cancelled, WorkOrderStatuses.Pending) => true,
            _ => false
        };

        return isAllowed
            ? null
            : $"A work order cannot transition from {currentStatus} to {targetStatus}.";
    }
}
