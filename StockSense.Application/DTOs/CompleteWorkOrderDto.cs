using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public sealed class CompleteWorkOrderDto
{
    [Required, MaxLength(20)]
    public string PaymentMethod { get; set; } = "Cash";

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }
}
