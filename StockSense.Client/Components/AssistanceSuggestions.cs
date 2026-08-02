namespace StockSense.Client.Components;

public sealed record AssistanceSuggestion(string Label, string Prompt);

public static class AssistanceSuggestions
{
    public static IReadOnlyList<AssistanceSuggestion> ForRole(string? role) => role switch
    {
        "Admin" =>
        [
            new("Sales overview", "Show me today's sales summary."),
            new("Inventory counts", "Which products are low or out of stock?"),
            new("Supplier status", "Show supplier and pending order status."),
            new("Operations overview", "Summarize current appointments and active builds.")
        ],
        "Employee" =>
        [
            new("Product availability", "Check current product availability."),
            new("Today's appointments", "Show today's appointment schedule."),
            new("Active builds", "Show active motorcycle build requests."),
            new("Compatibility lookup", "Help me identify compatible parts for a motorcycle.")
        ],
        _ =>
        [
            new("Product availability", "Which products are currently available?"),
            new("Services offered", "What services do you offer?"),
            new("Appointment availability", "What appointment slots are available?"),
            new("Build quotations", "What build quotation options are available?"),
            new("Compatibility", "Which available parts fit my motorcycle?")
        ]
    };
}
