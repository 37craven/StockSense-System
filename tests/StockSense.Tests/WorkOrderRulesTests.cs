using StockSense.Domain.Entities;

namespace StockSense.Tests;

public sealed class WorkOrderRulesTests
{
    [Theory]
    [InlineData(WorkOrderStatuses.Pending, WorkOrderStatuses.Confirmed)]
    [InlineData(WorkOrderStatuses.Pending, WorkOrderStatuses.Cancelled)]
    [InlineData(WorkOrderStatuses.Confirmed, WorkOrderStatuses.Cancelled)]
    public void ValidNonCheckoutTransition_IsAccepted(string current, string target)
    {
        Assert.Null(WorkOrderRules.ValidateStatusTransition(current, target));
    }

    [Theory]
    [InlineData(WorkOrderStatuses.Pending, WorkOrderStatuses.Completed)]
    [InlineData(WorkOrderStatuses.Confirmed, WorkOrderStatuses.Completed)]
    [InlineData(WorkOrderStatuses.Completed, WorkOrderStatuses.Pending)]
    [InlineData(WorkOrderStatuses.Completed, WorkOrderStatuses.Confirmed)]
    [InlineData(WorkOrderStatuses.Cancelled, WorkOrderStatuses.Pending)]
    public void CompletionAndTerminalReopen_AreRejectedByGenericStatusFlow(string current, string target)
    {
        Assert.NotNull(WorkOrderRules.ValidateStatusTransition(current, target));
    }
}
