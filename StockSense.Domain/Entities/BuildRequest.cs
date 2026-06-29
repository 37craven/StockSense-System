using System.ComponentModel.DataAnnotations.Schema;
namespace StockSense.Domain.Entities;

public class BuildRequest
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string BuildName { get; set; } = "Custom Build"; // e.g., "My Drag Setup"
    public string SelectedPartsJson { get; set; } = string.Empty; // We will store IDs as a simple JSON string
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Pending";
}