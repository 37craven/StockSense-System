namespace StockSense.Client.Components;

public sealed record AssistanceSuggestion(string Label, string Prompt);

public static class AssistanceSuggestions
{
    public static IReadOnlyList<AssistanceSuggestion> ForRole(string? role) => role switch
    {
        "Admin" =>
        [
            new("Sales performance", "Show the sales summary for the last 30 days."),
            new("Replenishment settings", "Show safety-stock confidence and automatic-order settings."),
            new("Pending supplier orders", "Show pending and partially received supplier orders."),
            new("User overview", "Show user counts by role.")
        ],
        "Employee" =>
        [
            new("Inventory attention", "Which products are low or out of stock?"),
            new("Appointment status", "Summarize current appointment status."),
            new("Build status", "Summarize current motorcycle build status."),
            new("Incoming orders", "Show pending and partially received supplier orders."),
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
