using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace StockSense.Domain.Entities;

public class Appointment
{
    public int Id { get; set; }
    // Customer Info
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerUserId { get; set; }
    public string? ContactNumber { get; set; }
    // Date and Time
    public DateTime AppointmentDate { get; set; } = DateTime.Now;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string TimeSlot { get; set; } = string.Empty; // Stores "09:00", "10:15", etc.
    // Service Details
    public string ServicesRequested { get; set; } = string.Empty; // Stores "Change Oil, Air Filter Cleaning"
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; } // Stores the calculated P900
    public string? SelectedProductsJson { get; set; }
    public string Status { get; set; } = "Pending";
    public string Category { get; set; } = "General";
    public string MechanicName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
    public int? BuildRequestId { get; set; }
    public BuildRequest? BuildRequest { get; set; }
    public int? MotorcycleId { get; set; }
    public Motorcycle? Motorcycle { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
