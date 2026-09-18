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
    [AllowAnonymous]
    public async Task<ActionResult<List<MechanicDto>>> GetActiveMechanics()
    {
        var mechanics = await _repo.GetActiveAsync();
        var dtos = mechanics.Select(m => new MechanicDto { Id = m.Id, Name = m.Name, IsActive = m.IsActive, InactiveFrom = m.InactiveFrom, InactiveUntil = m.InactiveUntil, InactiveReason = m.InactiveReason }).ToList();
        return Ok(dtos);
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<MechanicDto>>> GetAllMechanics()
    {
        var mechanics = await _repo.GetAllAsync();
        var dtos = mechanics.Select(m => new MechanicDto { Id = m.Id, Name = m.Name, IsActive = m.IsActive, InactiveFrom = m.InactiveFrom, InactiveUntil = m.InactiveUntil, InactiveReason = m.InactiveReason }).ToList();
        return Ok(dtos);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateMechanic([FromBody] MechanicDto dto)
    {
        var normalizedName = dto.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedName))
            return BadRequest(ApiResponse.Error("Mechanic name is required."));
        if (await _repo.NameExistsAsync(normalizedName))
            return Conflict(ApiResponse.Error($"A mechanic named \"{normalizedName}\" already exists."));

        var mechanic = new Mechanic { Name = normalizedName, IsActive = dto.IsActive, InactiveFrom = dto.InactiveFrom, InactiveUntil = dto.InactiveUntil, InactiveReason = dto.InactiveReason };
        // Validate inactive range if provided
        var rangeError = ValidateInactiveRange(mechanic);
        if (rangeError != null) return BadRequest(ApiResponse.Error(rangeError));
        await _repo.AddAsync(mechanic);
        await _repo.SaveChangesAsync();
        return Ok(new MechanicDto { Id = mechanic.Id, Name = mechanic.Name, IsActive = mechanic.IsActive, InactiveFrom = mechanic.InactiveFrom, InactiveUntil = mechanic.InactiveUntil, InactiveReason = mechanic.InactiveReason });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateMechanic(int id, [FromBody] MechanicDto dto)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound(ApiResponse.NotFound("Mechanic"));

        var normalizedName = dto.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedName))
            return BadRequest(ApiResponse.Error("Mechanic name is required."));
        if (await _repo.NameExistsAsync(normalizedName, id))
            return Conflict(ApiResponse.Error($"A mechanic named \"{normalizedName}\" already exists."));

        existing.Name = normalizedName;
        existing.IsActive = dto.IsActive;
        existing.InactiveFrom = dto.InactiveFrom;
        existing.InactiveUntil = dto.InactiveUntil;
        existing.InactiveReason = dto.InactiveReason?.Trim();
        var rangeError = ValidateInactiveRange(existing);
        if (rangeError != null) return BadRequest(ApiResponse.Error(rangeError));
        // If manually activated, clear range
        if (existing.IsActive)
        {
            existing.InactiveFrom = null;
            existing.InactiveUntil = null;
            existing.InactiveReason = null;
        }
        await _repo.UpdateAsync(existing);
        await _repo.SaveChangesAsync();
        return Ok();
    }

    private static string? ValidateInactiveRange(Mechanic m)
    {
        if (m.IsActive && (m.InactiveFrom != null || m.InactiveUntil != null))
            return "Active mechanics cannot have an inactive date range. Clear the dates or deactivate.";
        if (!m.IsActive)
        {
            if (m.InactiveFrom == null || m.InactiveUntil == null)
                return "Inactive mechanics require a date range (From and Until). For indefinite, set a far future Until date.";
            if (m.InactiveFrom.Value.Date > m.InactiveUntil.Value.Date)
                return "Inactive From date cannot be after Until date.";
            if (m.InactiveUntil.Value.Date < DateTime.Today)
                return "Inactive Until date cannot be in the past.";
            if ((m.InactiveUntil.Value.Date - m.InactiveFrom.Value.Date).TotalDays > 365)
                return "Inactive range cannot exceed 365 days.";
            if (string.IsNullOrWhiteSpace(m.InactiveReason))
                return "Reason is required when deactivating a mechanic.";
            if (m.InactiveReason.Length > 150)
                return "Reason cannot exceed 150 characters.";
        }
        return null;
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
