namespace StockSense.Domain.Entities;

public class Motorcycle
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string BaseCC { get; set; } = string.Empty;
}
