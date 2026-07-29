using System.ComponentModel.DataAnnotations;

namespace StockSense.Web.Options;

public sealed class ChatbotOptions
{
    public const string SectionName = "Chatbot";

    [Required]
    public Uri BaseUrl { get; init; } = new("http://127.0.0.1:8000/");

    [Range(1, 120)]
    public int TimeoutSeconds { get; init; } = 30;

    public bool HasSupportedScheme() =>
        BaseUrl.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        || BaseUrl.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
