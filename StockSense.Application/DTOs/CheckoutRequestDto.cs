using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public sealed class CheckoutRequestDto
{
    [Required, MinLength(1)]
    public List<CheckoutLineDto> Lines { get; set; } = [];

    [Required, MaxLength(20)]
    public string PaymentMethod { get; set; } = "Cash";

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }
}

public sealed class CheckoutLineDto
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 999_999)]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal DiscountAmount { get; set; }

    // Denied-attempt logger — optional, revert by restoring original file
    public int? RequestedQuantity { get; set; }

    [Range(0, 999_999)]
    public int LostSalesQuantity { get; set; }
}
