using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public class CreateSupplierDto
{
    [Required(ErrorMessage = "Supplier name is required.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Supplier name cannot exceed 50 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "Enter a valid Philippine mobile number (09XXXXXXXXX).")]
    public string MobileNumber { get; set; } = string.Empty;
}
