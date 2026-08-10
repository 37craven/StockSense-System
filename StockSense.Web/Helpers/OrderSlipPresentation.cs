using StockSense.Domain.Entities;

namespace StockSense.Web.Helpers;

public static class OrderSlipPresentation
{
    public static string Label(string status) => status switch
    {
        OrderSlipStatuses.PartiallyReceived => "Partial",
        OrderSlipStatuses.ClosedShort => "Closed short",
        _ => status
    };

    public static string BadgeClass(string status) => status switch
    {
        OrderSlipStatuses.Draft => "order-status--draft",
        OrderSlipStatuses.Approved => "order-status--approved",
        OrderSlipStatuses.Ordered => "order-status--ordered",
        OrderSlipStatuses.PartiallyReceived => "order-status--partial",
        OrderSlipStatuses.Completed => "order-status--completed",
        OrderSlipStatuses.ClosedShort => "order-status--closed-short",
        OrderSlipStatuses.Cancelled => "order-status--cancelled",
        _ => "order-status--unknown"
    };

    // Shared action styles keep the hierarchy identical in the list, details view and dialogs.
    public const string DetailsActionClass = "order-action order-action--details";
    public const string ApproveActionClass = "order-action order-action--approve";
    public const string SendActionClass = "order-action order-action--send";
    public const string ReceiveActionClass = "order-action order-action--receive";
    public const string WarningActionClass = "order-action order-action--warning";

    public const string EditableQuantityClass = "order-receipt-quantity";
}
