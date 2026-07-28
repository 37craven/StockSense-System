using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data.Repositories;
using StockSense.Infrastructure.Data;

namespace StockSense.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentRepository _repo;
    private readonly StoreServiceRepository _serviceRepo;
    private readonly IWorkOrderCheckoutService _checkoutService;
    private readonly UserManager<ApplicationUser> _userManager;
    private static readonly TimeZoneInfo PhZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

    public AppointmentsController(
        AppointmentRepository repo,
        StoreServiceRepository serviceRepo,
        IWorkOrderCheckoutService checkoutService,
        UserManager<ApplicationUser> userManager)
    {
        _repo = repo;
        _serviceRepo = serviceRepo;
        _checkoutService = checkoutService;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
    {
        try
        {
            var customer = await _userManager.GetUserAsync(User);
            if (customer is null) return Unauthorized();
            var customerEmail = customer.Email ?? string.Empty;
            var customerFullName = GetFullName(customer);
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
                CustomerName = customerFullName,
                CustomerEmail = customerEmail,
                CustomerUserId = customer.Id,
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
    [Authorize(Roles = "Employee,Admin")]
    public async Task<ActionResult<List<AppointmentDto>>> GetAllAppointments()
    {
        var appointments = await _repo.GetAllAsync();
        await EnrichCustomerIdentitiesAsync(appointments);
        var dtos = appointments.Select(a => MapToDto(a)).ToList();
        return Ok(dtos);
    }

    [HttpPut("{id}/assign-mechanic")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> AssignMechanic(int id, [FromBody] MechanicAssignmentDto assignment)
    {
        var appointment = await _repo.GetByIdAsync(id);
        if (appointment == null) return NotFound(ApiResponse.NotFound("Appointment"));

        var transitionError = WorkOrderRules.ValidateStatusTransition(
            appointment.Status,
            WorkOrderStatuses.Confirmed);
        if (transitionError is not null) return Conflict(ApiResponse.Error(transitionError));

        appointment.MechanicName = assignment.MechanicName;
        appointment.DurationMinutes = assignment.DurationMinutes;
        appointment.Status = WorkOrderStatuses.Confirmed;
        await _repo.UpdateAsync(appointment);
        return Ok(new { message = $"Assigned to {assignment.MechanicName}" });
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
    {
        var appointment = await _repo.GetByIdAsync(id);
        if (appointment == null) return NotFound(ApiResponse.NotFound("Appointment"));

        if (string.Equals(newStatus, WorkOrderStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Error("Use the completion checkout endpoint to complete an appointment."));

        var canonicalStatus = new[] { WorkOrderStatuses.Pending, WorkOrderStatuses.Confirmed, WorkOrderStatuses.Cancelled }
            .SingleOrDefault(value => string.Equals(value, newStatus?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (canonicalStatus is null) return BadRequest(ApiResponse.Error("Unsupported appointment status."));
        var transitionError = WorkOrderRules.ValidateStatusTransition(appointment.Status, canonicalStatus);
        if (transitionError is not null) return Conflict(ApiResponse.Error(transitionError));

        appointment.Status = canonicalStatus;
        await _repo.UpdateAsync(appointment);
        return Ok(new { message = "Status updated" });
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<ActionResult<ReceiptDto>> Complete(
        int id,
        [FromBody] CompleteWorkOrderDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var receipt = await _checkoutService.CompleteAppointmentAsync(
                id,
                request,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                InventoryDefaults.LocationId,
                cancellationToken);
            return Ok(receipt);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse.NotFound("Appointment"));
        }
        catch (JsonException)
        {
            return BadRequest(ApiResponse.Error("The appointment's selected-product data is invalid."));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiResponse.Error(exception.Message));
        }
    }

    [HttpGet("my-bookings")]
    public async Task<ActionResult<List<AppointmentDto>>> GetMyBookings()
    {
        var customer = await _userManager.GetUserAsync(User);
        if (customer is null) return Unauthorized();
        var appointments = await _repo.GetByCustomerIdentityAsync(
            customer.Id,
            customer.Email ?? string.Empty,
            GetFullName(customer));
        await EnrichCustomerIdentitiesAsync(appointments);
        var dtos = appointments.Select(a => MapToDto(a)).ToList();
        return Ok(dtos);
    }

    private static AppointmentDto MapToDto(Appointment a) => new()
    {
        Id = a.Id, CustomerName = a.CustomerName, CustomerEmail = a.CustomerEmail,
        ContactNumber = a.ContactNumber,
        AppointmentDate = a.AppointmentDate, CreatedAt = a.CreatedAt,
        TimeSlot = a.TimeSlot, ServicesRequested = a.ServicesRequested,
        SelectedProductsJson = a.SelectedProductsJson,
        TotalAmount = a.TotalAmount, Status = a.Status, Category = a.Category,
        MechanicName = a.MechanicName, DurationMinutes = a.DurationMinutes,
        CompletedAt = a.CompletedAt, TransactionId = a.TransactionId,
        InvoiceNumber = a.Transaction?.InvoiceNumber
    };

    private static string GetFullName(ApplicationUser user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName)
            ? user.Email?.Split('@')[0] ?? "Customer"
            : fullName;
    }

    private async Task EnrichCustomerIdentitiesAsync(IEnumerable<Appointment> appointments)
    {
        var users = await _userManager.Users.AsNoTracking().ToListAsync();
        foreach (var appointment in appointments)
        {
            var user = users.FirstOrDefault(value => value.Id == appointment.CustomerUserId)
                ?? users.FirstOrDefault(value =>
                    string.Equals(value.Email, appointment.CustomerEmail, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value.Email, appointment.CustomerName, StringComparison.OrdinalIgnoreCase))
                ?? users.FirstOrDefault(value =>
                    string.Equals(GetFullName(value), appointment.CustomerName, StringComparison.OrdinalIgnoreCase));
            if (user is null) continue;
            appointment.CustomerName = GetFullName(user);
            appointment.CustomerEmail = user.Email;
        }
    }

    private class ServiceProductBreakdown
    {
        public string ServiceName { get; set; } = "";
        public decimal ServicePrice { get; set; }
        public List<ProductItem> Products { get; set; } = new();
    }

    private class ProductItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public bool Selected { get; set; } = true;
    }
}
