using System.ComponentModel.DataAnnotations.Schema;
namespace StockSense.Domain.Entities;

public class BuildRequest
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerUserId { get; set; }
    public string BuildName { get; set; } = "Custom Build"; // e.g., "My Drag Setup"
    public string SelectedPartsJson { get; set; } = string.Empty; // We will store IDs as a simple JSON string
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Pending";
    public DateTime? CompletedAt { get; set; }
    public int? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
    public Appointment? Appointment { get; set; }
}
