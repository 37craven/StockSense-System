using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using StockSense.Application.DTOs;

namespace StockSense.Web.Services;

public interface IAssistanceClient
{
    Task<string> AskAsync(
        string message,
        string userRole,
        IReadOnlyList<AssistanceHistoryMessage> history,
        string correlationId,
        CancellationToken cancellationToken);
}

public sealed class AssistanceClient(HttpClient httpClient, ILogger<AssistanceClient> logger) : IAssistanceClient
{
    public async Task<string> AskAsync(
        string message,
        string userRole,
        IReadOnlyList<AssistanceHistoryMessage> history,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var endpoint = GetChatEndpoint(httpClient.BaseAddress);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new ChatbotRequest(message, userRole, history))
        };
        request.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Chatbot request failed with upstream status code {StatusCode}.",
                (int)response.StatusCode);
            throw new AssistanceUpstreamException();
        }

        try
        {
            var result = await response.Content.ReadFromJsonAsync<ChatbotResponse>(cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(result?.Reply))
            {
                logger.LogWarning("Chatbot returned an empty or invalid response.");
                throw new AssistanceUpstreamException();
            }

            return result.Reply;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Chatbot returned malformed JSON.");
            throw new AssistanceUpstreamException();
        }
    }

    private static Uri GetChatEndpoint(Uri? baseUrl)
    {
        if (baseUrl is null)
            throw new InvalidOperationException("The chatbot HTTP client has no base URL configured.");

        // Uri resolves a relative path by replacing the last path segment unless the
        // base ends in '/'. Normalize here so '/chatbot' remains '/chatbot/api/chat'.
        var normalizedBaseUrl = new Uri($"{baseUrl.AbsoluteUri.TrimEnd('/')}/", UriKind.Absolute);
        return new Uri(normalizedBaseUrl, "api/chat");
    }

    private sealed record ChatbotRequest(
        string Message,
        [property: JsonPropertyName("user_role")] string UserRole,
        IReadOnlyList<AssistanceHistoryMessage> History);
    private sealed record ChatbotResponse(string Reply);
}

public sealed class AssistanceUpstreamException : Exception
{
}
