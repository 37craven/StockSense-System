namespace StockSense.Client.Components;

public sealed record AssistanceRoleCopy(
    string AssistantName,
    string Heading,
    string Description,
    string Placeholder,
    string HelperText);

public static class AssistanceRolePresentation
{
    public static AssistanceRoleCopy ForRole(string? role) => role switch
    {
        "Admin" => new(
            "StockSense Management Assistant",
            "What would you like to review?",
            "Review sales, replenishment, supplier orders, user totals, and operational settings.",
            "Ask about sales, inventory, orders, users, or settings...",
            "Reports reflect the latest recorded system data."),
        "Employee" => new(
            "StockSense Operations Assistant",
            "How can I help with today's work?",
            "Check inventory attention items, appointment and build status, incoming orders, or part compatibility.",
            "Ask about today's operations...",
            "Operational results reflect the latest recorded system data."),
        _ => new(
            "Sap Shop Assistant",
            "How can I help?",
            "Book appointments, check product availability, browse services, get quotes, start custom builds, or manage your account.",
            "Ask Sap Shop Assistant...",
            "Stock availability may change.")
    };
}
