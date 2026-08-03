using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public class CreateSupplierDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(50)]
    public string MobileNumber { get; set; } = string.Empty;
}
