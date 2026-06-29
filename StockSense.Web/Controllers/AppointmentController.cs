using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data.Repositories;

namespace StockSense.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentRepository _repo;
    private readonly StoreServiceRepository _serviceRepo;
    private static readonly TimeZoneInfo PhZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

    public AppointmentsController(AppointmentRepository repo, StoreServiceRepository serviceRepo)
    {
        _repo = repo;
        _serviceRepo = serviceRepo;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
    {
        try
        {
            string flatServices = string.Join(", ", dto.SelectedServices);
            var matchedServices = await _serviceRepo.GetByNamesAsync(dto.SelectedServices);
            decimal serviceTotal = matchedServices.Sum(s => s.Price);
            decimal productTotal = 0m;

            if (!string.IsNullOrWhiteSpace(dto.SelectedProductsJson))
            {
                var breakdown = JsonSerializer.Deserialize<List<ServiceProductBreakdown>>(dto.SelectedProductsJson);
                if (breakdown != null)
                    productTotal = breakdown.Sum(s => s.Products.Where(p => p.Selected).Sum(p => p.Price));
            }

            int totalDuration = matchedServices.Sum(s => s.EstimatedMinutes);
            DateTime phNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhZone);

            var appointment = new Appointment
            {
                CustomerName = dto.CustomerName,
                ContactNumber = dto.ContactNumber,
                AppointmentDate = DateTime.SpecifyKind(dto.AppointmentDate.Date, DateTimeKind.Unspecified),
                TimeSlot = dto.TimeSlot,
                Category = string.IsNullOrWhiteSpace(dto.Category) ? "General Service" : dto.Category,
                ServicesRequested = flatServices,
                SelectedProductsJson = dto.SelectedProductsJson,
                Status = "Pending",
                CreatedAt = phNow,
                TotalAmount = serviceTotal + productTotal,
                DurationMinutes = totalDuration,
                MechanicName = "Any Available"
            };

            var saved = await _repo.AddAsync(appointment);
            return Ok(new { message = "Appointment booked successfully!", id = saved.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.Error(ex.Message));
        }
    }

    [HttpGet("booked-slots")]
    public async Task<IActionResult> GetBookedSlots([FromQuery] DateTime date, [FromQuery] string? mechanic)
    {
        var appointments = await _repo.GetAppointmentsByDateAndMechanicAsync(date, mechanic);
        var slots = appointments.Select(a => new BookedSlotDto { TimeSlot = a.TimeSlot, EstimatedMinutes = a.DurationMinutes }).ToList();
        return Ok(slots);
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<AppointmentDto>>> GetAllAppointments()
    {
        var appointments = await _repo.GetAllAsync();
        var dtos = appointments.Select(a => MapToDto(a)).ToList();
        return Ok(dtos);
    }

    [HttpPut("{id}/assign-mechanic")]
    public async Task<IActionResult> AssignMechanic(int id, [FromBody] MechanicAssignmentDto assignment)
    {
        var appointment = await _repo.GetByIdAsync(id);
        if (appointment == null) return NotFound(ApiResponse.NotFound("Appointment"));

        appointment.MechanicName = assignment.MechanicName;
        appointment.DurationMinutes = assignment.DurationMinutes;
        appointment.Status = "Confirmed";
        await _repo.UpdateAsync(appointment);
        return Ok(new { message = $"Assigned to {assignment.MechanicName}" });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
    {
        var appointment = await _repo.GetByIdAsync(id);
        if (appointment == null) return NotFound(ApiResponse.NotFound("Appointment"));

        appointment.Status = newStatus;
        await _repo.UpdateAsync(appointment);
        return Ok(new { message = "Status updated" });
    }

    [HttpGet("my-bookings")]
    public async Task<ActionResult<List<AppointmentDto>>> GetMyBookings([FromQuery] string name)
    {
        var appointments = await _repo.GetByCustomerNameAsync(name);
        var dtos = appointments.Select(a => MapToDto(a)).ToList();
        return Ok(dtos);
    }

    private static AppointmentDto MapToDto(Appointment a) => new()
    {
        Id = a.Id, CustomerName = a.CustomerName, ContactNumber = a.ContactNumber,
        AppointmentDate = a.AppointmentDate, CreatedAt = a.CreatedAt,
        TimeSlot = a.TimeSlot, ServicesRequested = a.ServicesRequested,
        SelectedProductsJson = a.SelectedProductsJson,
        TotalAmount = a.TotalAmount, Status = a.Status, Category = a.Category,
        MechanicName = a.MechanicName, DurationMinutes = a.DurationMinutes
    };

    private class ServiceProductBreakdown
    {
        public string ServiceName { get; set; } = "";
        public decimal ServicePrice { get; set; }
        public List<ProductItem> Products { get; set; } = new();
    }

    private class ProductItem
    {
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public bool Selected { get; set; } = true;
    }
}
