using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public class UpdateProductDto
{
    [Required]
    public int Id { get; set; }

    [Range(0.01, 9_999_999)]
    public decimal Price { get; set; }

    [Range(0, 99_999)]
    public int ReorderTarget { get; set; }

    [Range(0, 999_999)]
    public int CurrentStock { get; set; }
}
