using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using StockSense.Domain.Entities;

namespace StockSense.Web.Services;

public sealed class PayMongoService(HttpClient httpClient, ILogger<PayMongoService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<(string LinkId, string CheckoutUrl)> CreatePaymentLinkAsync(
        decimal amountPesos,
        string description,
        string referenceNumber,
        CancellationToken ct = default)
    {
        long amountCentavos = (long)Math.Round(amountPesos * 100m, MidpointRounding.AwayFromZero);

        var requestBody = JsonSerializer.Serialize(new
        {
            data = new
            {
                attributes = new
                {
                    amount = amountCentavos,
                    description,
                    reference_number = referenceNumber
                }
            }
        }, JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, "links")
        {
            Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json")
        };
        using var response = await httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning(
                "PayMongo link creation returned {StatusCode}: {Body}",
                (int)response.StatusCode, errorBody);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new InvalidOperationException("PayMongo API key is invalid or expired. Update the key in appsettings.");

            response.EnsureSuccessStatusCode();
        }

        var rawJson = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(rawJson);

        var data = document.RootElement.GetProperty("data");
        var linkId = data.GetProperty("id").GetString();
        var checkoutUrl = data.GetProperty("attributes").GetProperty("checkout_url").GetString();

        if (string.IsNullOrWhiteSpace(linkId) || string.IsNullOrWhiteSpace(checkoutUrl))
            throw new InvalidOperationException("PayMongo link response is missing id or checkout_url.");

        return (linkId, checkoutUrl);
    }

    public async Task<string?> GetLinkStatusAsync(string linkId, CancellationToken ct = default)
    {
        try
        {
            using var response = await httpClient.GetAsync($"links/{linkId}", ct);
            response.EnsureSuccessStatusCode();

            var rawJson = await response.Content.ReadAsStringAsync(ct);
            using var document = JsonDocument.Parse(rawJson);

            var status = document.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("status").GetString();
            return status;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or KeyNotFoundException or OperationCanceledException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Failed to fetch PayMongo link status for {LinkId}.", linkId);
            return null;
        }
    }

    public async Task<(string? Status, string? CheckoutUrl)> GetLinkDetailsAsync(string linkId, CancellationToken ct = default)
    {
        try
        {
            using var response = await httpClient.GetAsync($"links/{linkId}", ct);
            response.EnsureSuccessStatusCode();

            var rawJson = await response.Content.ReadAsStringAsync(ct);
            using var document = JsonDocument.Parse(rawJson);

            var attrs = document.RootElement.GetProperty("data").GetProperty("attributes");
            var status = attrs.GetProperty("status").GetString();
            var checkoutUrl = attrs.TryGetProperty("checkout_url", out var urlEl) ? urlEl.GetString() : null;
            return (status, checkoutUrl);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or KeyNotFoundException or OperationCanceledException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Failed to fetch PayMongo link details for {LinkId}.", linkId);
            return (null, null);
        }
    }

    /// <summary>
    /// Verifies a PayMongo webhook signature using HMAC-SHA256.
    /// Returns true if the signature is valid; false otherwise.
    /// </summary>
    public static bool VerifyWebhookSignature(string payload, string? headerSignature, string webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(headerSignature) || string.IsNullOrWhiteSpace(webhookSecret))
            return false;

        using var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(webhookSecret));
        var computed = Convert.ToHexString(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload)));
        return string.Equals(computed, headerSignature, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Maps a PayMongo link status string to an internal PaymentStatuses constant.
    /// </summary>
    public static string MapLinkStatus(string? paymongoStatus) => paymongoStatus?.ToLowerInvariant() switch
    {
        "paid" => PaymentStatuses.Paid,
        "expired" => PaymentStatuses.Expired,
        "failed" => PaymentStatuses.Failed,
        _ => PaymentStatuses.AwaitingPayment
    };
}
