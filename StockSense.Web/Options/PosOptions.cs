namespace StockSense.Web.Options;

public sealed class PosOptions
{
    public const string SectionName = "Pos";
    public const string DefaultLocationId = "MAIN";

    public string LocationId { get; set; } = DefaultLocationId;
}
