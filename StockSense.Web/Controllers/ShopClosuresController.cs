using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

namespace StockSense.Web.Controllers;

[Route("api/shop-closures")]
[ApiController]
public class ShopClosuresController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ShopClosuresController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private static TimeZoneInfo PhZone
    {
        get
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"); }
            catch { try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila"); } catch { return TimeZoneInfo.Utc; } }
        }
    }
    private static DateTime PhToday => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhZone).Date;
    private static DateTime PhNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhZone);

    private static ShopClosureDto Map(ShopClosure c) => new()
    {
        Id = c.Id,
        StartDateTime = c.StartDateTime,
        EndDateTime = c.EndDateTime,
        Reason = c.Reason,
        IsWholeDay = c.IsWholeDay,
        CreatedBy = c.CreatedBy,
        CreatedAt = c.CreatedAt
    };

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ShopClosureDto>>> GetUpcoming()
    {
        var today = PhToday;
        var closures = await _context.ShopClosures
            .Where(c => c.EndDateTime.Date >= today)
            .OrderBy(c => c.StartDateTime)
            .ToListAsync();
        return Ok(closures.Select(Map).ToList());
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ShopClosureDto>>> GetAll()
    {
        var closures = await _context.ShopClosures.OrderBy(c => c.StartDateTime).ToListAsync();
        return Ok(closures.Select(Map).ToList());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateShopClosureDto dto)
    {
        var err = Validate(dto);
        if (err != null) return BadRequest(ApiResponse.Error(err));
        var (start, end) = BuildDateTimes(dto);
        var closure = new ShopClosure
        {
            StartDateTime = start,
            EndDateTime = end,
            Reason = dto.Reason.Trim(),
            IsWholeDay = dto.IsWholeDay,
            CreatedBy = User.Identity?.Name,
            CreatedAt = PhNow
        };
        _context.ShopClosures.Add(closure);
        await _context.SaveChangesAsync();
        return Ok(Map(closure));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateShopClosureDto dto)
    {
        var closure = await _context.ShopClosures.FindAsync(id);
        if (closure == null) return NotFound(ApiResponse.NotFound("Closure"));
        var err = Validate(dto);
        if (err != null) return BadRequest(ApiResponse.Error(err));
        var (start, end) = BuildDateTimes(dto);
        closure.StartDateTime = start;
        closure.EndDateTime = end;
        closure.Reason = dto.Reason.Trim();
        closure.IsWholeDay = dto.IsWholeDay;
        await _context.SaveChangesAsync();
        return Ok(Map(closure));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var closure = await _context.ShopClosures.FindAsync(id);
        if (closure == null) return NotFound(ApiResponse.NotFound("Closure"));
        _context.ShopClosures.Remove(closure);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse.Success("Closure deleted"));
    }

    private static (DateTime start, DateTime end) BuildDateTimes(CreateShopClosureDto dto)
    {
        DateTime start, end;
        if (dto.IsWholeDay)
        {
            start = dto.StartDate.Date;
            end = dto.EndDate.Date.AddDays(1).AddTicks(-1); // 23:59:59
        }
        else
        {
            var sTime = dto.StartTime ?? TimeSpan.Zero;
            var eTime = dto.EndTime ?? new TimeSpan(23, 59, 0);
            start = dto.StartDate.Date + sTime;
            end = dto.EndDate.Date + eTime;
        }
        return (start, end);
    }

    private string? Validate(CreateShopClosureDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason)) return "Reason is required.";
        if (dto.Reason.Trim().Length > 500) return "Reason cannot exceed 500 characters.";
        if (dto.StartDate == default || dto.EndDate == default) return "Start and End date are required.";
        var (start, end) = BuildDateTimes(dto);
        if (start > end) return "Start cannot be after End.";
        if (end.Date < PhToday) return "End date cannot be in the past.";
        if ((end.Date - start.Date).TotalDays > 365) return "Range cannot exceed 365 days.";
        if (!dto.IsWholeDay)
        {
            if (dto.StartTime == null || dto.EndTime == null) return "Start and End time are required for partial closure.";
            if (dto.StartDate.Date == dto.EndDate.Date && dto.StartTime.Value >= dto.EndTime.Value)
                return "Start time must be before End time.";
        }
        return null;
    }
}
