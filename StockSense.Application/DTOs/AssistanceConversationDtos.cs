namespace StockSense.Application.DTOs;

public static class AssistanceConversationLimits
{
    public const int MaxHistoryMessages = 12;
    public const int MaxMessageLength = 8000;
    public const int MaxHistoryCharacters = 32_000;
}

public sealed record AssistanceHistoryMessage(string Role, string Content);

public sealed record WorkflowState(
    string? WorkflowType,
    string? CurrentStep,
    Dictionary<string, object>? CollectedData,
    string Status,
    string? ConfirmationSummary,
    string? PreviousWorkflowType,
    Dictionary<string, object>? PreviousCollectedData);

public sealed record ChatAction(
    string Label,
    string ActionType,
    string? Url,
    string? Prompt,
    string? WorkflowType,
    string? Icon,
    Dictionary<string, string>? BookingData);
