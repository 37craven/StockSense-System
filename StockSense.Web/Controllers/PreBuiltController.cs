using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data.Repositories;

namespace StockSense.Web.Controllers;

[Route("api/prebuilts")]
[ApiController]
[Authorize]
public class PreBuiltController : ControllerBase
{
    private readonly PreBuiltRepository _repo;

    public PreBuiltController(PreBuiltRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<List<PreBuiltPackageDto>>> GetMatchingPackages(
        [FromQuery] string brand, [FromQuery] string model,
        [FromQuery] string cc, [FromQuery] decimal minBudget, [FromQuery] decimal maxBudget)
    {
        var allPackages = await _repo.GetAllAsync();
        var matching = allPackages
            .Where(p => p.IsActive && p.TotalPrice >= minBudget && p.TotalPrice <= maxBudget)
            .Where(p => p.CompatibleMotors.Any(m =>
                m.Brand.Equals(brand, StringComparison.OrdinalIgnoreCase) &&
                m.Model.Equals(model, StringComparison.OrdinalIgnoreCase) &&
                m.StockCC.Equals(cc, StringComparison.OrdinalIgnoreCase)))
            .Select(MapToDto).ToList();
        return Ok(matching);
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<PreBuiltPackageDto>>> GetAllPackages()
    {
        var packages = await _repo.GetAllAsync();
        return Ok(packages.Select(MapToDto).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> CreatePreBuilt([FromBody] CreatePreBuiltDto dto)
    {
        if (dto.SelectedProductIds == null || !dto.SelectedProductIds.Any())
            return BadRequest(ApiResponse.Error("A package must contain at least one product."));

        var selectedProducts = await _repo.GetProductsByIdsAsync(dto.SelectedProductIds);
        var package = new PreBuiltPackage
        {
            Name = dto.Name, Description = dto.Description,
            MinAddedCC = dto.MinAddedCC, MaxAddedCC = dto.MaxAddedCC,
            IsActive = true,
            CompatibleMotors = dto.CompatibleMotors.Select(m => new PreBuiltPackageMotor
            {
                Brand = m.Brand, Model = m.Model, StockCC = m.StockCC
            }).ToList(),
            IncludedProducts = selectedProducts
        };

        await _repo.AddAsync(package);
        return Ok(MapToDto(package));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePreBuilt(int id, [FromBody] CreatePreBuiltDto dto)
    {
        var package = await _repo.GetByIdAsync(id);
        if (package == null) return NotFound(ApiResponse.NotFound("Package"));

        package.Name = dto.Name; package.Description = dto.Description;
        package.MinAddedCC = dto.MinAddedCC; package.MaxAddedCC = dto.MaxAddedCC;
        package.CompatibleMotors = dto.CompatibleMotors.Select(m => new PreBuiltPackageMotor
        {
            Brand = m.Brand, Model = m.Model, StockCC = m.StockCC
        }).ToList();
        package.IncludedProducts = await _repo.GetProductsByIdsAsync(dto.SelectedProductIds);

        await _repo.UpdateAsync(package);
        return Ok(MapToDto(package));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePreBuilt(int id)
    {
        var package = await _repo.GetByIdAsync(id);
        if (package == null) return NotFound(ApiResponse.NotFound("Package"));
        await _repo.DeleteAsync(id);
        return Ok(ApiResponse.Success("Package deleted."));
    }

    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var package = await _repo.GetByIdAsync(id);
        if (package == null) return NotFound(ApiResponse.NotFound("Package"));
        package.IsActive = !package.IsActive;
        await _repo.UpdateAsync(package);
        return Ok(ApiResponse.Success("Package toggled."));
    }

    private static PreBuiltPackageDto MapToDto(PreBuiltPackage p) => new()
    {
        Id = p.Id, Name = p.Name, Description = p.Description,
        MinAddedCC = p.MinAddedCC, MaxAddedCC = p.MaxAddedCC,
        IsActive = p.IsActive, TotalPrice = p.TotalPrice,
        CompatibleMotors = p.CompatibleMotors.Select(m => new CompatibleMotorDto
        {
            Id = m.Id, Brand = m.Brand, Model = m.Model, StockCC = m.StockCC
        }).ToList(),
        IncludedProducts = p.IncludedProducts.Select(prod => new PreBuiltProductDto
        {
            Id = prod.Id, Name = prod.Name, Brand = prod.Brand, Price = prod.Price
        }).ToList()
    };
}
