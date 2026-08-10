using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Services;

public static class OrderSlipMath
{
    public static bool IsOpenOrderStatus(string status) => status is
        OrderSlipStatuses.Draft or OrderSlipStatuses.Approved or OrderSlipStatuses.Ordered
        or OrderSlipStatuses.PartiallyReceived;

    public static bool CountsAsIncoming(string status) => status is
        OrderSlipStatuses.Approved or OrderSlipStatuses.Ordered or OrderSlipStatuses.PartiallyReceived;

    public static int ResolveOrderedQuantity(int orderedQuantity, int legacyQuantity) =>
        orderedQuantity > 0 ? orderedQuantity : legacyQuantity;

    public static int CalculateRemainingQuantity(
        int orderedQuantity, int legacyQuantity, int receivedQuantity) =>
        Math.Max(0, ResolveOrderedQuantity(orderedQuantity, legacyQuantity) - receivedQuantity);

    public static string? ValidateReceiptDate(DateTime receivedAt, DateTime? orderedAt, DateTime today)
    {
        if (receivedAt.Date > today.Date) return "Receipt date cannot be in the future.";
        if (orderedAt.HasValue && receivedAt.Date < orderedAt.Value.Date)
            return "Receipt date cannot be earlier than the date the order was placed.";
        return null;
    }

    public static IReadOnlyCollection<int> ResolveReceiptRecalculationProductIds(
        IEnumerable<int> affectedProductIds,
        IEnumerable<int> supplierProductIds,
        bool isCompleted)
    {
        ArgumentNullException.ThrowIfNull(affectedProductIds);
        ArgumentNullException.ThrowIfNull(supplierProductIds);
        var result = affectedProductIds.Distinct().ToHashSet();
        if (isCompleted) result.UnionWith(supplierProductIds);
        return result;
    }

    public static int CalculateSuggestedQuantity(
        int targetStock,
        int reorderPoint,
        int inventoryPosition,
        int minimumOrderQuantity,
        int packageSize,
        int? maximumStockLevel)
    {
        if (targetStock < 0) throw new ArgumentOutOfRangeException(nameof(targetStock));
        if (reorderPoint < 0) throw new ArgumentOutOfRangeException(nameof(reorderPoint));
        if (minimumOrderQuantity < 1) throw new ArgumentOutOfRangeException(nameof(minimumOrderQuantity));
        if (packageSize < 1) throw new ArgumentOutOfRangeException(nameof(packageSize));

        if (inventoryPosition > reorderPoint) return 0;

        var shortage = Math.Max(0, targetStock - inventoryPosition);
        if (shortage == 0) return 0;

        var quantity = Math.Max(shortage, minimumOrderQuantity);
        quantity = checked(((quantity + packageSize - 1) / packageSize) * packageSize);

        if (maximumStockLevel.HasValue)
        {
            var capacity = Math.Max(0, maximumStockLevel.Value - inventoryPosition);
            quantity = Math.Min(quantity, capacity - capacity % packageSize);
        }

        return quantity >= minimumOrderQuantity ? quantity : 0;
    }

    public static string? ValidateOrderedQuantity(
        int quantity,
        int minimumOrderQuantity,
        int packageSize,
        int inventoryPosition,
        int? maximumStockLevel)
    {
        if (quantity <= 0) return "Ordered quantity must be greater than zero.";
        if (quantity < minimumOrderQuantity)
            return $"Ordered quantity must be at least the minimum order quantity of {minimumOrderQuantity}.";
        if (quantity % packageSize != 0)
            return $"Ordered quantity must be a multiple of the package size {packageSize}.";
        if (maximumStockLevel.HasValue && (long)inventoryPosition + quantity > maximumStockLevel.Value)
            return $"Ordered quantity would exceed the maximum stock level of {maximumStockLevel.Value}.";
        return null;
    }

    public static string? ValidateTransition(string currentStatus, string targetStatus)
    {
        var allowed = (currentStatus, targetStatus) switch
        {
            (OrderSlipStatuses.Draft, OrderSlipStatuses.Approved) => true,
            (OrderSlipStatuses.Approved, OrderSlipStatuses.Ordered) => true,
            (OrderSlipStatuses.Ordered, OrderSlipStatuses.PartiallyReceived) => true,
            (OrderSlipStatuses.Ordered, OrderSlipStatuses.Completed) => true,
            (OrderSlipStatuses.PartiallyReceived, OrderSlipStatuses.PartiallyReceived) => true,
            (OrderSlipStatuses.PartiallyReceived, OrderSlipStatuses.Completed) => true,
            (OrderSlipStatuses.PartiallyReceived, OrderSlipStatuses.ClosedShort) => true,
            _ => false
        };
        return allowed ? null : $"An order slip cannot transition from {currentStatus} to {targetStatus}.";
    }

    public static string? ValidateCloseShort(
        string status, bool hasReceivedItems, bool hasRemainingItems, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return "A reason is required to close the remaining order.";
        if (status != OrderSlipStatuses.PartiallyReceived)
            return "Only a partially received order can be closed with items outstanding.";
        if (!hasReceivedItems) return "At least one item must have been received before closing the remaining order.";
        if (!hasRemainingItems) return "This order has no remaining items to close.";
        return null;
    }

    public static string? ValidateCancellation(string status, bool hasReceivedItems, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return "A cancellation reason is required.";
        if (status is OrderSlipStatuses.Completed or OrderSlipStatuses.ClosedShort or OrderSlipStatuses.Cancelled)
            return "A completed, closed, or cancelled order slip cannot be cancelled.";
        if (status is not (OrderSlipStatuses.Draft or OrderSlipStatuses.Approved
            or OrderSlipStatuses.Ordered or OrderSlipStatuses.PartiallyReceived))
            return $"An order slip with status {status} cannot be cancelled.";
        return null;
    }
}
