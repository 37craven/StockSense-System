namespace StockSense.Domain.Entities;

public class Mechanic
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

