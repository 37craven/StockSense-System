using System.ComponentModel.DataAnnotations;

namespace StockSense.Application.DTOs;

public class CompatibleMotorDto
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string StockCC { get; set; } = string.Empty;
}

public class PreBuiltPackageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MinAddedCC { get; set; }
    public int MaxAddedCC { get; set; }
    public bool IsActive { get; set; }
    public decimal TotalPrice { get; set; }

    public List<CompatibleMotorDto> CompatibleMotors { get; set; } = new();
    public List<PreBuiltProductDto> IncludedProducts { get; set; } = new();
}

public class PreBuiltProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class CreatePreBuiltDto
{
    [Required(ErrorMessage = "Package name is required.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 500)]
    public int MinAddedCC { get; set; }

    [Range(0, 500)]
    public int MaxAddedCC { get; set; }

    [Required(ErrorMessage = "At least one compatible motor is required.")]
    [MinLength(1)]
    public List<CompatibleMotorDto> CompatibleMotors { get; set; } = new();

    [Required(ErrorMessage = "At least one product must be selected.")]
    [MinLength(1)]
    public List<int> SelectedProductIds { get; set; } = new();
}
