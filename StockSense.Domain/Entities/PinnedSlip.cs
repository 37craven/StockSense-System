namespace StockSense.Domain.Entities;

public class PinnedSlip
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string SlipData { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
