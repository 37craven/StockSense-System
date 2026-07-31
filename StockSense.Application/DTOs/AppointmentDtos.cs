using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public class AppointmentDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? ContactNumber { get; set; }
    public DateTime AppointmentDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public string ServicesRequested { get; set; } = string.Empty;
    public string? SelectedProductsJson { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string MechanicName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? TransactionId { get; set; }
    public string? InvoiceNumber { get; set; }
}

public partial class CreateAppointmentDto
{
    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Phone]
    public string? ContactNumber { get; set; }

    [Required]
    public DateTime AppointmentDate { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Time slot is required.")]
    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Time slot must be in HH:mm format.")]
    public string TimeSlot { get; set; } = string.Empty;

    [Required(ErrorMessage = "At least one service must be selected.")]
    [MinLength(1)]
    public List<string> SelectedServices { get; set; } = new();

    [StringLength(100)]
    public string Category { get; set; } = "General";

    public string? SelectedProductsJson { get; set; }
}

public class BookedSlotDto
{
    public string TimeSlot { get; set; } = string.Empty;
    public int EstimatedMinutes { get; set; }
}

public class MechanicAssignmentDto
{
    [Required(ErrorMessage = "Mechanic name is required.")]
    [StringLength(100)]
    public string MechanicName { get; set; } = string.Empty;

    [Range(15, 480)]
    public int DurationMinutes { get; set; }
}

public class ScheduleAppointmentRequest
{
    [Required]
    public int MechanicId { get; set; }

    [Required]
    public string MechanicName { get; set; } = string.Empty;

    [Required]
    public DateTime AppointmentDate { get; set; }

    [Required]
    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Time slot must be in HH:mm format.")]
    public string TimeSlot { get; set; } = string.Empty;

    [Required]
    public string ServicesRequested { get; set; } = string.Empty;

    public int DurationMinutes { get; set; } = 120;
}
