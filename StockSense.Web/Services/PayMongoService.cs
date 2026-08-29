using System.Text.Json;
using System.Text.Json.Serialization;

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
        response.EnsureSuccessStatusCode();

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
}
