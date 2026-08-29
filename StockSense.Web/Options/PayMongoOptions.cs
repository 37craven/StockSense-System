using System.ComponentModel.DataAnnotations;

namespace StockSense.Web.Options;

public sealed class PayMongoOptions
{
    public const string SectionName = "PayMongo";

    [Required]
    public Uri BaseUrl { get; init; } = new("https://api.paymongo.com/v1");

    [Required]
    public string SecretKey { get; init; } = string.Empty;

    public bool Enabled { get; init; } = true;
}
