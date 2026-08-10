using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data.Repositories;

namespace StockSense.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MechanicsController : ControllerBase
{
    private readonly MechanicRepository _repo;

    public MechanicsController(MechanicRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<List<MechanicDto>>> GetActiveMechanics()
    {
        var mechanics = await _repo.GetActiveAsync();
        var dtos = mechanics.Select(m => new MechanicDto { Id = m.Id, Name = m.Name, IsActive = m.IsActive }).ToList();
        return Ok(dtos);
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<MechanicDto>>> GetAllMechanics()
    {
        var mechanics = await _repo.GetAllAsync();
        var dtos = mechanics.Select(m => new MechanicDto { Id = m.Id, Name = m.Name, IsActive = m.IsActive }).ToList();
        return Ok(dtos);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateMechanic([FromBody] MechanicDto dto)
    {
        var mechanic = new Mechanic { Name = dto.Name, IsActive = dto.IsActive };
        await _repo.AddAsync(mechanic);
        await _repo.SaveChangesAsync();
        return Ok(new MechanicDto { Id = mechanic.Id, Name = mechanic.Name, IsActive = mechanic.IsActive });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateMechanic(int id, [FromBody] MechanicDto dto)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound(ApiResponse.NotFound("Mechanic"));

        existing.Name = dto.Name;
        existing.IsActive = dto.IsActive;
        await _repo.UpdateAsync(existing);
        await _repo.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMechanic(int id)
    {
        var deleted = await _repo.DeleteAsync(id);
        if (!deleted) return NotFound(ApiResponse.NotFound("Mechanic"));
        await _repo.SaveChangesAsync();
        return Ok(ApiResponse.Success("Mechanic deleted successfully"));
    }
}
