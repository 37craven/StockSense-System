namespace StockSense.Application.DTOs;

public class BuildRequestDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string BuildName { get; set; } = string.Empty;
    public string SelectedPartsJson { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "Pending";
}
