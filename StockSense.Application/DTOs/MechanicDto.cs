using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public class MechanicDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mechanic name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
