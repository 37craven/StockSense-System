using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public class MechanicDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mechanic name is required.")]
    [StringLength(50, ErrorMessage = "Mechanic name cannot exceed 50 characters.")]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime? InactiveFrom { get; set; }

    public DateTime? InactiveUntil { get; set; }

    [StringLength(250, ErrorMessage = "Reason cannot exceed 250 characters.")]
    public string? InactiveReason { get; set; }
}
