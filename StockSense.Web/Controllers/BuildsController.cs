using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data.Repositories;

namespace StockSense.Web.Controllers;

[Route("api/builds")]
[ApiController]
[Authorize]
public class BuildsController : ControllerBase
{
    private readonly BuildRequestRepository _buildRepo;
    private readonly ProductRepository _productRepo;

    public BuildsController(BuildRequestRepository buildRepo, ProductRepository productRepo)
    {
        _buildRepo = buildRepo;
        _productRepo = productRepo;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBuild([FromBody] CreateBuildRequestDto dto)
    {
        if (dto == null) return BadRequest(ApiResponse.Error("Request is empty."));

        var request = new BuildRequest
        {
            CustomerName = dto.CustomerName,
            BuildName = dto.BuildName,
            SelectedPartsJson = dto.SelectedPartsJson,
            TotalPrice = dto.TotalPrice,
            CreatedAt = DateTime.Now,
            Status = "Pending"
        };

        await _buildRepo.AddAsync(request);
        await _buildRepo.SaveChangesAsync();

        var result = new BuildRequestDto
        {
            Id = request.Id, CustomerName = request.CustomerName, BuildName = request.BuildName,
            SelectedPartsJson = request.SelectedPartsJson, TotalPrice = request.TotalPrice,
            CreatedAt = request.CreatedAt, Status = request.Status
        };
        return Ok(result);
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<BuildRequestDto>>> GetAllBuilds()
    {
        var builds = await _buildRepo.GetAllAsync();
        var dtos = builds.Select(b => new BuildRequestDto
        {
            Id = b.Id, CustomerName = b.CustomerName, BuildName = b.BuildName,
            SelectedPartsJson = b.SelectedPartsJson, TotalPrice = b.TotalPrice,
            CreatedAt = b.CreatedAt, Status = b.Status
        }).ToList();
        return Ok(dtos);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
    {
        var build = await _buildRepo.GetByIdAsync(id);
        if (build == null) return NotFound(ApiResponse.NotFound("Build"));

        if (newStatus == "Completed" && build.Status != "Completed")
        {
            await DeductInventory(build);
        }

        build.Status = newStatus;
        await _buildRepo.SaveChangesAsync();
        return Ok();
    }

    private async Task DeductInventory(BuildRequest build)
    {
        if (string.IsNullOrEmpty(build.SelectedPartsJson)) return;
        try
        {
            var usedParts = JsonSerializer.Deserialize<List<BuildPartDto>>(
                build.SelectedPartsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            if (usedParts != null)
            {
                foreach (var part in usedParts)
                {
                    var dbProduct = await _productRepo.GetByIdAsync(part.Id);
                    if (dbProduct != null)
                    {
                        dbProduct.DeductStock(1);
                        await _productRepo.UpdateAsync(dbProduct);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deduct inventory: {ex.Message}");
        }
    }

    [HttpGet("customer/{userName}")]
    public async Task<ActionResult<List<BuildRequestDto>>> GetCustomerBuilds(string userName)
    {
        if (string.IsNullOrEmpty(userName)) return BadRequest(ApiResponse.Error("User name is required."));
        var builds = await _buildRepo.GetByCustomerNameAsync(userName);
        var dtos = builds.Select(b => new BuildRequestDto
        {
            Id = b.Id, CustomerName = b.CustomerName, BuildName = b.BuildName,
            SelectedPartsJson = b.SelectedPartsJson, TotalPrice = b.TotalPrice,
            CreatedAt = b.CreatedAt, Status = b.Status
        }).ToList();
        return Ok(dtos);
    }
}
