using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public class CreateBuildRequestDto
{
    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(50, ErrorMessage = "Customer name cannot exceed 50 characters.")]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Build name cannot exceed 50 characters.")]
    public string BuildName { get; set; } = "Custom Build";

    [Required(ErrorMessage = "Selected parts are required.")]
    public string SelectedPartsJson { get; set; } = string.Empty;

    [Range(0, 1_000_000)]
    public decimal TotalPrice { get; set; }

    public int? MotorcycleId { get; set; }
}
