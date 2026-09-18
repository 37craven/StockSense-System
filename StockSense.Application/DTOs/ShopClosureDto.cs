using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public class ShopClosureDto
{
    public int Id { get; set; }

    [Required]
    public DateTime StartDateTime { get; set; }

    [Required]
    public DateTime EndDateTime { get; set; }

    [Required(ErrorMessage = "Reason is required.")]
    [StringLength(250, ErrorMessage = "Reason cannot exceed 250 characters.")]
    public string Reason { get; set; } = string.Empty;

    public bool IsWholeDay { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateShopClosureDto
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    [Required(ErrorMessage = "Reason is required.")]
    [StringLength(250, ErrorMessage = "Reason cannot exceed 250 characters.")]
    public string Reason { get; set; } = string.Empty;

    public bool IsWholeDay { get; set; }
}
