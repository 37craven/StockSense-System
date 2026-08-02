namespace StockSense.Application.DTOs;

public sealed class MotorcycleOptionDto
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string BaseCC { get; set; } = string.Empty;

    public string DisplayName => $"{Brand} {Model} ({BaseCC})";
}
