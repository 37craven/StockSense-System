namespace StockSense.Client.Components;

public sealed record AssistanceSuggestion(string Label, string Prompt, string Icon = "message-circle");

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
            new("Get a Quote", "I'd like a parts quote for my motorcycle.", "file-text"),
            new("Start Custom Build", "I want a custom build for my motorcycle.", "wrench"),
            new("View My Bookings", "Show my appointment bookings.", "list"),
            new("View My Builds", "Show my custom builds.", "package"),
            new("Account Settings", "Take me to my account settings.", "user")
        ]
    };
}
