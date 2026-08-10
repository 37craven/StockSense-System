using StockSense.Domain.Entities;
using StockSense.Infrastructure.Services;

namespace StockSense.Tests;

public sealed class OrderSlipMathTests
{
    [Fact]
    public void AtReorderPoint_TriggersOrderToTarget()
    {
        var quantity = OrderSlipMath.CalculateSuggestedQuantity(30, 10, 10, 1, 1, null);

        Assert.Equal(20, quantity);
    }

    [Fact]
    public void AboveReorderPoint_DoesNotOrderEvenWhenBelowTarget()
    {
        var quantity = OrderSlipMath.CalculateSuggestedQuantity(30, 10, 11, 1, 1, null);

        Assert.Equal(0, quantity);
    }

    [Fact]
    public void IncomingStockInInventoryPosition_PreventsDuplicateNeed()
    {
        const int currentStock = 4;
        const int incomingStock = 8;

        var quantity = OrderSlipMath.CalculateSuggestedQuantity(
            30, 10, currentStock + incomingStock, 1, 1, null);

        Assert.Equal(0, quantity);
    }

    [Theory]
    [InlineData(OrderSlipStatuses.Draft, true, false)]
    [InlineData(OrderSlipStatuses.Approved, true, true)]
    [InlineData(OrderSlipStatuses.Ordered, true, true)]
    [InlineData(OrderSlipStatuses.PartiallyReceived, true, true)]
    [InlineData(OrderSlipStatuses.Completed, false, false)]
    [InlineData(OrderSlipStatuses.ClosedShort, false, false)]
    [InlineData(OrderSlipStatuses.Cancelled, false, false)]
    public void StatusClassification_SeparatesDuplicatePreventionFromIncoming(
        string status, bool isOpen, bool countsAsIncoming)
    {
        Assert.Equal(isOpen, OrderSlipMath.IsOpenOrderStatus(status));
        Assert.Equal(countsAsIncoming, OrderSlipMath.CountsAsIncoming(status));
    }

    [Fact]
    public void LegacyOrderedQuantity_FallsBackToQuantity()
    {
        Assert.Equal(7, OrderSlipMath.ResolveOrderedQuantity(0, 7));
        Assert.Equal(9, OrderSlipMath.ResolveOrderedQuantity(9, 7));
    }

    [Fact]
    public void MinimumOrderQuantity_IsAppliedBeforePackageCeiling()
    {
        var quantity = OrderSlipMath.CalculateSuggestedQuantity(12, 10, 9, 10, 6, null);

        Assert.Equal(12, quantity);
    }

    [Fact]
    public void Shortage_IsRoundedUpToPackageSize()
    {
        var quantity = OrderSlipMath.CalculateSuggestedQuantity(25, 10, 8, 1, 6, null);

        Assert.Equal(18, quantity);
    }

    [Fact]
    public void MaximumStockLevel_CapsAtWholePackage()
    {
        var quantity = OrderSlipMath.CalculateSuggestedQuantity(40, 15, 10, 5, 6, 27);

        Assert.Equal(12, quantity);
    }

    [Fact]
    public void MaximumStockLevel_ReturnsNoOrderWhenNoValidMinimumFits()
    {
        var quantity = OrderSlipMath.CalculateSuggestedQuantity(40, 15, 10, 10, 6, 19);

        Assert.Equal(0, quantity);
    }

    [Theory]
    [InlineData(9, 10, 5, 0, null)]
    [InlineData(12, 10, 5, 0, "multiple")]
    [InlineData(20, 10, 5, 25, "maximum")]
    public void QuantityValidation_EnforcesOrderingRules(
        int quantity, int minimum, int package, int maximum, string? expectedErrorFragment)
    {
        var error = OrderSlipMath.ValidateOrderedQuantity(
            quantity, minimum, package, inventoryPosition: 10, maximum == 0 ? null : maximum);

        if (expectedErrorFragment is null)
            Assert.Contains("minimum", error, StringComparison.OrdinalIgnoreCase);
        else
            Assert.Contains(expectedErrorFragment, error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(OrderSlipStatuses.Draft, OrderSlipStatuses.Approved)]
    [InlineData(OrderSlipStatuses.Approved, OrderSlipStatuses.Ordered)]
    [InlineData(OrderSlipStatuses.Ordered, OrderSlipStatuses.PartiallyReceived)]
    [InlineData(OrderSlipStatuses.PartiallyReceived, OrderSlipStatuses.Completed)]
    [InlineData(OrderSlipStatuses.PartiallyReceived, OrderSlipStatuses.ClosedShort)]
    public void ValidStatusTransitions_AreAccepted(string current, string target)
    {
        Assert.Null(OrderSlipMath.ValidateTransition(current, target));
    }

    [Theory]
    [InlineData(OrderSlipStatuses.Draft, OrderSlipStatuses.Ordered)]
    [InlineData(OrderSlipStatuses.Approved, OrderSlipStatuses.Completed)]
    [InlineData(OrderSlipStatuses.Completed, OrderSlipStatuses.Approved)]
    [InlineData(OrderSlipStatuses.Cancelled, OrderSlipStatuses.Draft)]
    public void InvalidStatusTransitions_AreRejected(string current, string target)
    {
        Assert.NotNull(OrderSlipMath.ValidateTransition(current, target));
    }

    [Theory]
    [InlineData(10, 0, 4, 6)]
    [InlineData(0, 8, 3, 5)]
    [InlineData(5, 0, 7, 0)]
    public void RemainingQuantity_UsesOrderedQuantityAndNeverGoesNegative(
        int ordered, int legacy, int received, int expected)
    {
        Assert.Equal(expected, OrderSlipMath.CalculateRemainingQuantity(ordered, legacy, received));
    }

    [Theory]
    [InlineData(OrderSlipStatuses.PartiallyReceived, true, true, "Supplier cannot fulfill", true)]
    [InlineData(OrderSlipStatuses.PartiallyReceived, true, true, "", false)]
    [InlineData(OrderSlipStatuses.Ordered, true, true, "Supplier cannot fulfill", false)]
    [InlineData(OrderSlipStatuses.PartiallyReceived, false, true, "Supplier cannot fulfill", false)]
    [InlineData(OrderSlipStatuses.PartiallyReceived, true, false, "Supplier cannot fulfill", false)]
    public void CloseShort_RequiresPartialReceiptOutstandingItemsAndReason(
        string status, bool hasReceived, bool hasRemaining, string reason, bool valid)
    {
        var error = OrderSlipMath.ValidateCloseShort(status, hasReceived, hasRemaining, reason);
        Assert.Equal(valid, error is null);
    }

    [Fact]
    public void Cancellation_RequiresReason()
    {
        Assert.Contains("reason", OrderSlipMath.ValidateCancellation(OrderSlipStatuses.Draft, false, ""),
            StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(OrderSlipStatuses.Completed, false)]
    [InlineData(OrderSlipStatuses.Cancelled, false)]
    [InlineData(OrderSlipStatuses.Ordered, true)]
    public void Cancellation_RejectsOnlyTerminalOrders(string status, bool hasReceivedItems)
    {
        var result = OrderSlipMath.ValidateCancellation(status, hasReceivedItems, "No longer needed");

        if (status is OrderSlipStatuses.Completed or OrderSlipStatuses.Cancelled) Assert.NotNull(result);
        else Assert.Null(result);
    }

    [Theory]
    [InlineData(OrderSlipStatuses.Ordered, false)]
    [InlineData(OrderSlipStatuses.PartiallyReceived, true)]
    public void Cancellation_AllowsActiveOrderedStatusesWithReason(string status, bool hasReceivedItems)
    {
        Assert.Null(OrderSlipMath.ValidateCancellation(status, hasReceivedItems, "Supplier cannot fulfill remainder"));
    }

    [Fact]
    public void SupplierGroups_UseIndependentInventoryInputs()
    {
        var supplierOne = OrderSlipMath.CalculateSuggestedQuantity(30, 10, 10, 1, 5, null);
        var supplierTwo = OrderSlipMath.CalculateSuggestedQuantity(50, 20, 20, 1, 10, null);

        Assert.Equal(20, supplierOne);
        Assert.Equal(30, supplierTwo);
    }

    [Fact]
    public void ReceiptDate_RejectsFutureAndPreOrderDates()
    {
        var today = new DateTime(2026, 7, 26);

        Assert.Contains("future", OrderSlipMath.ValidateReceiptDate(today.AddDays(1), today, today),
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("earlier", OrderSlipMath.ValidateReceiptDate(today.AddDays(-1), today, today),
            StringComparison.OrdinalIgnoreCase);
        Assert.Null(OrderSlipMath.ValidateReceiptDate(today, today, today));
    }

    [Fact]
    public void PartialReceipt_RecalculatesOnlyAffectedProducts()
    {
        var ids = OrderSlipMath.ResolveReceiptRecalculationProductIds([2, 2], [1, 2, 3], false);

        Assert.Equal([2], ids.Order());
    }

    [Fact]
    public void CompletedReceipt_RecalculatesAllSupplierProducts()
    {
        var ids = OrderSlipMath.ResolveReceiptRecalculationProductIds([2], [1, 2, 3], true);

        Assert.Equal([1, 2, 3], ids.Order());
    }
}
