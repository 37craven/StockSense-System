using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data.Repositories;

namespace StockSense.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly StoreServiceRepository _serviceRepo;
    private readonly ProductRepository _productRepo;

    public ServicesController(StoreServiceRepository serviceRepo, ProductRepository productRepo)
    {
        _serviceRepo = serviceRepo;
        _productRepo = productRepo;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetServices()
    {
        var services = await _serviceRepo.GetAllWithProductsAsync();
        var dtos = services.Select(s => new StoreServiceDto
        {
            Id = s.Id, Name = s.Name, Price = s.Price, Category = s.Category,
            EstimatedMinutes = s.EstimatedMinutes, Status = s.Status,
            RequiredProducts = s.RequiredProducts.Select(p => new ProductDto(
                p.Id, p.Name, p.Category, p.Brand, p.Price, p.CurrentStock,
                p.ReorderTarget, p.SupplierId ?? 0, p.Supplier?.Name ?? "", p.ImageUrl ?? "",
                IsActive: p.IsActive
            )).ToList()
        }).ToList();
        return Ok(dtos);
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventory()
    {
        var products = await _productRepo.GetAllAsync();
        var dtos = products.Select(p => new ProductDto(
            p.Id, p.Name, p.Category, p.Brand, p.Price, p.CurrentStock,
            p.ReorderTarget, p.SupplierId ?? 0, p.Supplier?.Name ?? "", p.ImageUrl ?? "",
            IsActive: p.IsActive)).ToList();
        return Ok(dtos);
    }

    [HttpPost]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> CreateService([FromBody] CreateStoreServiceDto dto)
    {
        var normalizedName = dto.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedName))
            return BadRequest(ApiResponse.Error("Service name is required."));
        if (await _serviceRepo.NameExistsAsync(normalizedName))
            return Conflict(ApiResponse.Error($"A service named \"{normalizedName}\" already exists."));

        var service = new StoreService
        {
            Name = normalizedName,
            Price = dto.Price,
            Category = dto.Category?.Trim() ?? string.Empty,
            EstimatedMinutes = dto.EstimatedMinutes,
            Status = "Active"
        };
        await _serviceRepo.AddAsync(service);
        return Ok();
    }

    [HttpPost("update-products")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> UpdateServiceProducts([FromBody] UpdateServiceProductsDto dto)
    {
        var service = await _serviceRepo.GetByIdWithProductsAsync(dto.ServiceId);
        if (service == null) return NotFound(ApiResponse.NotFound("Service"));

        service.Price = dto.Price;
        service.RequiredProducts = await _productRepo.GetByIdsAsync(dto.ProductIds);
        await _serviceRepo.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> UpdateServiceStatus(int id, [FromBody] UpdateServiceStatusDto dto)
    {
        var status = new[] { "Active", "Inactive" }
            .SingleOrDefault(value => string.Equals(value, dto.Status?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (status is null)
            return BadRequest(ApiResponse.Error("Unsupported service status."));

        var service = await _serviceRepo.GetByIdWithProductsAsync(id);
        if (service == null) return NotFound(ApiResponse.NotFound("Service"));

        service.Status = status;
        await _serviceRepo.SaveChangesAsync();
        return Ok(new { message = status == "Active" ? "Service activated." : "Service deactivated." });
    }
}
