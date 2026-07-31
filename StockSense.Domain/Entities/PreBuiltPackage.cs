using System.ComponentModel.DataAnnotations.Schema;
namespace StockSense.Domain.Entities;

public class PreBuiltPackage
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MinAddedCC { get; set; }
    public int MaxAddedCC { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PreBuiltPackageMotor> CompatibleMotors { get; set; } = new();
    public List<Product> IncludedProducts { get; set; } = new();
    [NotMapped] public decimal TotalPrice => IncludedProducts.Sum(p => p.Price);
}
