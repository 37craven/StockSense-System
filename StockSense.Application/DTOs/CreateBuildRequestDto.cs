using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public class CreateBuildRequestDto
{
    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(200)]
    public string BuildName { get; set; } = "Custom Build";

    [Required(ErrorMessage = "Selected parts are required.")]
    public string SelectedPartsJson { get; set; } = string.Empty;

    [Range(0, 1_000_000)]
    public decimal TotalPrice { get; set; }

    [Required(ErrorMessage = "Select a motorcycle.")]
    [Range(1, int.MaxValue, ErrorMessage = "Select a valid motorcycle.")]
    public int? MotorcycleId { get; set; }
}
