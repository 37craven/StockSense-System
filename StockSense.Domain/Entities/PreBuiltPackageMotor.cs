namespace StockSense.Domain.Entities;

public class PreBuiltPackageMotor
{
    public int Id { get; set; }
    public int PreBuiltPackageId { get; set; }
    public int? MotorcycleId { get; set; }
    public Motorcycle? Motorcycle { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string StockCC { get; set; } = string.Empty;
}
