using System.ComponentModel.DataAnnotations;
using StockSense.Domain.Entities;

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
    public string? SelectedServicesJson { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string MechanicName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? TransactionId { get; set; }
    public string? PaymentLinkId { get; set; }
    public string PaymentStatus { get; set; } = PaymentStatuses.NotRequired;
    public decimal? PaymentAmount { get; set; }
    public string? InvoiceNumber { get; set; }
    public int? MotorcycleId { get; set; }
    public MotorcycleOptionDto? Motorcycle { get; set; }
}

public partial class CreateAppointmentDto
{
    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(50, ErrorMessage = "Customer name cannot exceed 50 characters.")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact number is required.")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "Enter a valid Philippine mobile number (09XXXXXXXXX).")]
    public string ContactNumber { get; set; } = string.Empty;

    [Required]
    public DateTime AppointmentDate { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Time slot is required.")]
    [StringLength(5, MinimumLength = 5, ErrorMessage = "Time slot must be in HH:mm format.")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Time slot must be in HH:mm format (00:00-23:59).")]
    public string TimeSlot { get; set; } = string.Empty;

    [Required(ErrorMessage = "At least one service must be selected.")]
    [MinLength(1)]
    public List<string> SelectedServices { get; set; } = new();

    [StringLength(100)]
    public string Category { get; set; } = "General";

    [StringLength(50, ErrorMessage = "Mechanic name cannot exceed 50 characters.")]
    public string? MechanicName { get; set; }

    public string? SelectedProductsJson { get; set; }

    public string? SelectedServicesJson { get; set; }

    public int? MotorcycleId { get; set; }
}

public class BookedSlotDto
{
    public string TimeSlot { get; set; } = string.Empty;
    public int EstimatedMinutes { get; set; }
}

public class MechanicAssignmentDto
{
    [Required(ErrorMessage = "Mechanic name is required.")]
    [StringLength(50, ErrorMessage = "Mechanic name cannot exceed 50 characters.")]
    public string MechanicName { get; set; } = string.Empty;

    [Range(15, 480)]
    public int DurationMinutes { get; set; }
}

public class UpdateAppointmentDetailsDto
{
    public DateTime AppointmentDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string MechanicName { get; set; } = string.Empty;
    public string? AdminUserId { get; set; }
    public string? AdminEmail { get; set; }
    public string? AdminPin { get; set; }
    public string? Reason { get; set; }
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
    [StringLength(5, MinimumLength = 5, ErrorMessage = "Time slot must be in HH:mm format.")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Time slot must be in HH:mm format (00:00-23:59).")]
    public string TimeSlot { get; set; } = string.Empty;

    [Required]
    public string ServicesRequested { get; set; } = string.Empty;

    public int DurationMinutes { get; set; } = 120;
}
