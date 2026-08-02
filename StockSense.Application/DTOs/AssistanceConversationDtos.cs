namespace StockSense.Application.DTOs;

public static class AssistanceConversationLimits
{
    public const int MaxHistoryMessages = 12;
    public const int MaxMessageLength = 8000;
    public const int MaxHistoryCharacters = 32_000;
}

public sealed record AssistanceHistoryMessage(string Role, string Content);
