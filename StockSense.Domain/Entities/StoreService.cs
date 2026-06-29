using System.ComponentModel.DataAnnotations.Schema;
namespace StockSense.Domain.Entities;

public class StoreService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    public string Category { get; set; } = "General";
    public int EstimatedMinutes { get; set; }
    public string Status { get; set; } = "Active";
    public virtual ICollection<Product> RequiredProducts { get; set; } = new List<Product>();
}
