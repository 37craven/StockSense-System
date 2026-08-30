namespace StockSense.Client.Components;

public sealed record AssistanceSuggestion(string Label, string Prompt, string Icon = "message-circle", string? Url = null);

public static class AssistanceSuggestions
{
    public static IReadOnlyList<AssistanceSuggestion> ForRole(string? role) => role switch
    {
        "Admin" =>
        [
            new("Sales performance", "Show the sales summary for the last 30 days.", "bar-chart"),
            new("Replenishment settings", "Show safety-stock confidence and automatic-order settings.", "shield"),
            new("Pending supplier orders", "Show pending and partially received supplier orders.", "truck"),
            new("User overview", "Show user counts by role.", "users")
        ],
        "Employee" =>
        [
            new("Inventory attention", "Which products are low or out of stock?", "package"),
            new("Appointment status", "Summarize current appointment status.", "calendar"),
            new("Build status", "Summarize current motorcycle build status.", "wrench"),
            new("Incoming orders", "Show pending and partially received supplier orders.", "truck"),
            new("Compatibility lookup", "Help me identify compatible parts for a motorcycle.", "search")
        ],
        _ =>
        [
            new("Book Appointment", "I want to book an appointment.", "calendar"),
            new("Browse Services", "What services do you offer?", "cog"),
            new("Check Availability", "Which products are currently available?", "search"),
            new("Start Custom Build", "", "wrench", "/build"),
            new("Motorcycle Compatibility", "Check motorcycle part compatibility.", "bike"),
            new("View My Bookings", "show my appointments", "list"),
            new("View My Builds", "show my builds", "package")
        ]
    };
}
