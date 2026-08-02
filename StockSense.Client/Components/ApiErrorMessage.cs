using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StockSense.Client.Components;

/// <summary>
/// Converts API and transport failures into short messages that are safe to show in the UI.
/// HTTP status codes and exception details remain available to server/client diagnostics.
/// </summary>
public static partial class ApiErrorMessage
{
    private const string DefaultFailure = "Something went wrong. Please try again.";

    public static async Task<string> FromResponseAsync(
        HttpResponseMessage response,
        string? fallback = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests
            || response.StatusCode >= HttpStatusCode.InternalServerError)
            return FromStatus(response.StatusCode, fallback);

        var candidate = await TryReadMessageAsync(response, cancellationToken);
        return IsSafeMessage(candidate)
            ? Normalize(candidate!)
            : FromStatus(response.StatusCode, fallback);
    }

    public static string FromStatus(HttpStatusCode statusCode, string? fallback = null) => statusCode switch
    {
        HttpStatusCode.BadRequest => "Check the information and try again.",
        HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
        HttpStatusCode.Forbidden => "You do not have permission to do that.",
        HttpStatusCode.NotFound => "The requested item was not found.",
        HttpStatusCode.Conflict => "This information changed. Reload and try again.",
        HttpStatusCode.UnprocessableEntity => "Check the highlighted information and try again.",
        HttpStatusCode.TooManyRequests => "Too many requests. Wait a moment and try again.",
        >= HttpStatusCode.InternalServerError => SafeFallback(fallback),
        _ => SafeFallback(fallback)
    };

    public static string FromException(Exception exception, string? fallback = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            UserFacingApiException safe => safe.UserMessage,
            OperationCanceledException => "The request was cancelled.",
            HttpRequestException => "Unable to reach the server. Check your connection and try again.",
            JsonException => "The server returned an unexpected response. Please try again.",
            _ => SafeFallback(fallback)
        };
    }

    public static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string? fallback = null,
        CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
            return;

        throw new UserFacingApiException(
            await FromResponseAsync(response, fallback, cancellationToken));
    }

    private static async Task<string?> TryReadMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content) || content.Length > 16_384)
                return null;

            try
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.String)
                    return root.GetString();
                if (root.ValueKind != JsonValueKind.Object)
                    return null;

                foreach (var name in new[] { "error", "message", "detail" })
                {
                    if (TryGetString(root, name, out var value))
                        return value;
                }

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .SelectMany(property => property.Value.ValueKind == JsonValueKind.Array
                            ? property.Value.EnumerateArray()
                                .Where(item => item.ValueKind == JsonValueKind.String)
                                .Select(item => item.GetString())
                            : [])
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Take(3)
                        .ToArray();
                    if (messages.Length > 0)
                        return string.Join(" ", messages);
                }

                return TryGetString(root, "title", out var title) ? title : null;
            }
            catch (JsonException)
            {
                return content;
            }
        }
        catch (Exception exception) when (exception is JsonException or HttpRequestException or OperationCanceledException)
        {
            return null;
        }
    }

    private static bool TryGetString(JsonElement root, string name, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
            return false;
        value = property.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string SafeFallback(string? fallback) =>
        IsSafeMessage(fallback) ? Normalize(fallback!) : DefaultFailure;

    private static string Normalize(string value)
    {
        var normalized = string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (normalized.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[6..].TrimStart();
        return normalized;
    }

    private static bool IsSafeMessage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 320)
            return false;

        var text = value.Trim();
        if (text.StartsWith('{') || text.StartsWith('[') || text.StartsWith('<'))
            return false;

        string[] statusLabels = ["Bad Request", "Unauthorized", "Forbidden", "Not Found", "Conflict", "Unprocessable Entity"];
        if (statusLabels.Any(label => text.Equals(label, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (HttpStatusPattern().IsMatch(text) || StackTracePattern().IsMatch(text))
            return false;

        string[] technicalMarkers =
        [
            "exception", "stack trace", "traceid", "status code", "reasonphrase",
            "system.", "microsoft.", "sqlserver", "inner exception", " at stockSense.",
            "internal server error"
        ];
        return !technicalMarkers.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex(@"\b(?:400|401|403|404|409|422|429|5\d\d)\b", RegexOptions.CultureInvariant)]
    private static partial Regex HttpStatusPattern();

    [GeneratedRegex(@"(?:\r?\n|^)\s*at\s+[\w.<>]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StackTracePattern();
}

public sealed class UserFacingApiException(string userMessage) : Exception(userMessage)
{
    public string UserMessage { get; } = userMessage;
}
