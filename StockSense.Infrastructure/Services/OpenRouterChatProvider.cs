using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StockSense.Infrastructure.Services;

public sealed class OpenRouterChatProvider : IAiChatProvider
{
    private static readonly HttpClient HttpClient = new();
    private readonly ILogger<OpenRouterChatProvider> _logger;
    private readonly string? _apiKey;
    private readonly string _model;

    public OpenRouterChatProvider(
        IConfiguration configuration,
        ILogger<OpenRouterChatProvider> logger)
    {
        _logger = logger;
        _apiKey = configuration["OpenRouter:ApiKey"];
        _model = configuration["OpenRouter:Model"] ?? "openrouter/free";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public Task<string?> GenerateGroundedAnswerAsync(
        string userQuestion,
        string intent,
        IReadOnlyList<RagMatch> matches,
        string localAnswer,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || matches.Count == 0)
            return Task.FromResult<string?>(null);

        var context = string.Join("\n\n", matches.Take(5).Select(match =>
            $"Source: {match.Document.Type} | {match.Document.Title}\n" +
            $"Price: {FormatMoney(match.Document.Price)}\n" +
            $"Stock: {FormatStock(match.Document.CurrentStock)}\n" +
            $"Duration: {FormatDuration(match.Document.DurationMinutes)}\n" +
            $"Record text: {match.Document.Text}"));

        var systemPrompt =
            "You are StockSense Assistant. Answer only using the verified StockSense records provided. " +
            "Do not invent product prices, stock, compatibility, tuning gains, mechanics, or appointment slots. " +
            "If the records do not contain the answer, say that StockSense cannot verify it yet. " +
            "If the user asks outside inventory, parts, motorcycle build compatibility, tuning, services, mechanics, or appointments, refuse briefly.";

        var userPrompt =
            $"Intent: {intent}\n" +
            $"User question: {userQuestion}\n\n" +
            $"Verified StockSense records:\n{context}\n\n" +
            $"Local fallback answer:\n{localAnswer}\n\n" +
            "Write a concise, helpful answer for the user. Mention important stock, price, compatibility, gain, or appointment details when present.";

        return SendChatCompletionRequest(
            systemPrompt,
            userPrompt,
            temperature: 0.2,
            maxTokens: 600,
            failureMessage: "OpenRouter grounded answer generation failed. Falling back to local StockSense response.",
            cancellationToken);
    }

    public Task<string?> GenerateGeneralMotorcycleAnswerAsync(
        string userQuestion,
        string intent,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return Task.FromResult<string?>(null);

        var systemPrompt =
            "You are StockSense Assistant for a motorcycle shop. You may answer general motorcycle, maintenance, tuning, parts, symptoms, and build questions using general knowledge. " +
            "Be concise and practical. For safety-critical issues, advise inspection by a qualified mechanic. " +
            "Do not claim StockSense has a product, stock quantity, price, exact compatibility, mechanic, or appointment slot unless verified StockSense records are provided. " +
            "If the user asks for current StockSense stock, price, exact shop availability, or appointments and no records are provided, clearly say you cannot verify that from StockSense records yet.";

        var userPrompt =
            $"Intent: {intent}\n" +
            $"User question: {userQuestion}\n\n" +
            "Answer naturally. If the question asks for StockSense-specific inventory/price/appointment facts, explain that those need live StockSense records.";

        return SendChatCompletionRequest(
            systemPrompt,
            userPrompt,
            temperature: 0.35,
            maxTokens: 650,
            failureMessage: "OpenRouter general motorcycle answer generation failed. Falling back to local StockSense response.",
            cancellationToken);
    }

    private async Task<string?> SendChatCompletionRequest(
        string systemPrompt,
        string userPrompt,
        double temperature,
        int maxTokens,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://stocksense.local");
            request.Headers.TryAddWithoutValidation("X-Title", "StockSense System");
            request.Content = JsonContent.Create(new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature,
                max_tokens = maxTokens
            });

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                SafeLogWarning("OpenRouter request failed with {StatusCode}. Check the configured OpenRouter API key and model.", response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var text = ExtractText(json.RootElement);
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
        catch (Exception exception)
        {
            SafeLogWarning(exception, failureMessage);
            return null;
        }
    }

    private static string? ExtractText(JsonElement root)
    {
        if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var choice in choices.EnumerateArray())
        {
            if (!choice.TryGetProperty("message", out var message) ||
                !message.TryGetProperty("content", out var content))
                continue;

            var text = content.GetString();
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return null;
    }

    private void SafeLogWarning(string message, params object[] args)
    {
        try
        {
            _logger.LogWarning(message, args);
        }
        catch
        {
            Console.WriteLine(message);
        }
    }

    private void SafeLogWarning(Exception exception, string message)
    {
        try
        {
            _logger.LogWarning(exception, message);
        }
        catch
        {
            Console.WriteLine($"{message} {exception.Message}");
        }
    }

    private static string FormatMoney(decimal? value) => value.HasValue ? $"PHP {value:N0}" : "Not listed";
    private static string FormatStock(int? value) => value.HasValue ? value.Value.ToString() : "Not listed";
    private static string FormatDuration(int? value) => value.HasValue ? $"{value} minutes" : "Not listed";
}
