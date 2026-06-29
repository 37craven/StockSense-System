using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public class PreBuiltPackageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CompatibleBrand { get; set; } = string.Empty;
    public string CompatibleModel { get; set; } = string.Empty;
    public string TargetCC { get; set; } = string.Empty;
    public int EstimatedAddedCC { get; set; }
    public bool IsActive { get; set; }
    public decimal TotalPrice { get; set; } 
    
    public List<PreBuiltProductDto> IncludedProducts { get; set; } = new();
}

public class PreBuiltProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class CreatePreBuiltDto
{
    [Required(ErrorMessage = "Package name is required.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Compatible brand is required.")]
    [StringLength(100)]
    public string CompatibleBrand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Compatible model is required.")]
    [StringLength(100)]
    public string CompatibleModel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Target CC is required.")]
    [StringLength(50)]
    public string TargetCC { get; set; } = string.Empty;

    [Range(0, 500)]
    public int EstimatedAddedCC { get; set; }
    
    [Required(ErrorMessage = "At least one product must be selected.")]
    [MinLength(1)]
    public List<int> SelectedProductIds { get; set; } = new(); 
}