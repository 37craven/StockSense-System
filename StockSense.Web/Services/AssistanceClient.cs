using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using StockSense.Application.DTOs;

namespace StockSense.Web.Services;

public interface IAssistanceClient
{
    Task<(string Reply, IReadOnlyList<ChatAction>? Actions, WorkflowState? WorkflowState)> AskAsync(
        string message,
        string userRole,
        IReadOnlyList<AssistanceHistoryMessage> history,
        WorkflowState? workflowState,
        string customerName,
        string customerEmail,
        string customerUserId,
        string correlationId,
        CancellationToken cancellationToken);
}

public sealed class AssistanceClient(HttpClient httpClient, ILogger<AssistanceClient> logger) : IAssistanceClient
{
    public async Task<(string Reply, IReadOnlyList<ChatAction>? Actions, WorkflowState? WorkflowState)> AskAsync(
        string message,
        string userRole,
        IReadOnlyList<AssistanceHistoryMessage> history,
        WorkflowState? workflowState,
        string customerName,
        string customerEmail,
        string customerUserId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var endpoint = GetChatEndpoint(httpClient.BaseAddress);
        var snakeOpts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
        };
        var chatbotReq = new ChatbotRequest(message, userRole, history, workflowState, customerName, customerEmail, customerUserId);
        var requestBody = JsonSerializer.Serialize(chatbotReq, snakeOpts);
        logger.LogWarning("[ASSIST-CLIENT] Sending to chatbot: {Body}", requestBody.Length > 500 ? requestBody[..500] : requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json")
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
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("[ASSIST-CLIENT] Raw chatbot response: {Json}", rawJson.Length > 500 ? rawJson[..500] : rawJson);

            var result = JsonSerializer.Deserialize<ChatbotResponse>(
                rawJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });
            if (string.IsNullOrWhiteSpace(result?.Reply))
            {
                logger.LogWarning("Chatbot returned an empty or invalid response.");
                throw new AssistanceUpstreamException();
            }

            logger.LogWarning("[ASSIST-CLIENT] Deserialized: Reply={Reply} ActionCount={Count} Actions={Actions} WfStatus={WfStatus}",
                result.Reply?.Length > 80 ? result.Reply[..80] : result.Reply,
                result.Actions?.Count ?? 0,
                string.Join(", ", result.Actions?.Select(a => $"{a.ActionType}:{a.Label}") ?? []),
                result.WorkflowState?.Status);
            return (result.Reply, result.Actions, result.WorkflowState);
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
        IReadOnlyList<AssistanceHistoryMessage> History,
        [property: JsonPropertyName("workflow_state")] WorkflowState? WorkflowState,
        [property: JsonPropertyName("customer_name")] string CustomerName,
        [property: JsonPropertyName("customer_email")] string CustomerEmail,
        [property: JsonPropertyName("customer_user_id")] string CustomerUserId);
    private sealed record ChatbotResponse(
        string Reply,
        IReadOnlyList<ChatAction>? Actions,
        WorkflowState? WorkflowState);
}

public sealed class AssistanceUpstreamException : Exception
{
}
