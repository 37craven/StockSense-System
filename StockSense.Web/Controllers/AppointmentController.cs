using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Application.Exceptions;
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
    private readonly ApplicationDbContext _context;
    private readonly MotorcycleRepository _motorcycleRepository;
    private readonly ILogger<AppointmentsController> _logger;
    private static readonly TimeZoneInfo PhZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AppointmentsController(
        AppointmentRepository repo,
        StoreServiceRepository serviceRepo,
        IWorkOrderCheckoutService checkoutService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        MotorcycleRepository motorcycleRepository,
        ILogger<AppointmentsController> logger)
    {
        _repo = repo;
        _serviceRepo = serviceRepo;
        _checkoutService = checkoutService;
        _userManager = userManager;
        _context = context;
        _motorcycleRepository = motorcycleRepository;
        _logger = logger;
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

            Motorcycle? motorcycle = null;
            if (dto.MotorcycleId.HasValue)
            {
                motorcycle = await _motorcycleRepository.GetSelectableByIdAsync(dto.MotorcycleId.Value);
                if (motorcycle is null)
                    return BadRequest(ApiResponse.Error("The selected motorcycle does not exist."));
            }
            string flatServices = string.Join(", ", dto.SelectedServices);
            var matchedServices = await _serviceRepo.GetByNamesAsync(dto.SelectedServices);
            decimal serviceTotal = matchedServices.Sum(s => s.Price);
            decimal productTotal = 0m;

            if (!string.IsNullOrWhiteSpace(dto.SelectedProductsJson))
            {
                var breakdown = JsonSerializer.Deserialize<List<ServiceProductBreakdown>>(dto.SelectedProductsJson, JsonOpts);
                if (breakdown != null)
                    productTotal = breakdown.Sum(s => s.Products.Where(p => p.Selected).Sum(p => p.Price));
            }

            int totalDuration = matchedServices.Sum(s => s.EstimatedMinutes);
            DateTime phNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhZone);

            if (TimeSpan.TryParse(dto.TimeSlot, out var requestedStart))
            {
                var requestedEnd = requestedStart.Add(TimeSpan.FromMinutes(totalDuration));
                var existing = await _repo.GetAppointmentsByDateAndMechanicAsync(dto.AppointmentDate, null);
                var conflict = existing.Any(a =>
                    TimeSpan.TryParse(a.TimeSlot, out var existingStart) &&
                    (requestedStart < existingStart.Add(TimeSpan.FromMinutes(a.DurationMinutes))) &&
                    (requestedEnd > existingStart));
                if (conflict)
                    return Conflict(ApiResponse.Error("The selected time slot overlaps with an existing booking."));
            }

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
                MechanicName = "Any Available",
                MotorcycleId = motorcycle?.Id
            };

            var saved = await _repo.AddAsync(appointment);
            return Ok(new { message = "Appointment booked successfully!", id = saved.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Creating an appointment failed.");
            return StatusCode(500, ApiResponse.Error("The appointment could not be booked. Please try again."));
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
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null) return NotFound(ApiResponse.NotFound("Appointment"));

        if (appointment.TransactionId.HasValue)
        {
            var txn = await _context.Transactions
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == appointment.TransactionId.Value);
            appointment.Transaction = txn;
        }

        if (string.Equals(newStatus, WorkOrderStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Error("Use the completion checkout endpoint to complete an appointment."));

        var canonicalStatus = new[] { WorkOrderStatuses.Pending, WorkOrderStatuses.Confirmed, WorkOrderStatuses.Cancelled }
            .SingleOrDefault(value => string.Equals(value, newStatus?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (canonicalStatus is null) return BadRequest(ApiResponse.Error("Unsupported appointment status."));
        var transitionError = WorkOrderRules.ValidateStatusTransition(appointment.Status, canonicalStatus);
        if (transitionError is not null) return Conflict(ApiResponse.Error(transitionError));

        try
        {
            var wasConfirmed = appointment.Status == WorkOrderStatuses.Confirmed;

            if (canonicalStatus == WorkOrderStatuses.Pending && appointment.Transaction is not null)
            {
                await RestoreStockFromTransaction(appointment.Transaction);
                await _context.Transactions
                    .Where(t => t.Id == appointment.Transaction!.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsVoided, true));
                appointment.TransactionId = null;
                appointment.CompletedAt = null;
            }

            if (wasConfirmed && canonicalStatus != WorkOrderStatuses.Confirmed)
            {
                await ReleaseAppointmentReservations(appointment);
            }
            else if (canonicalStatus == WorkOrderStatuses.Confirmed && !wasConfirmed)
            {
                await ReserveAppointmentProducts(appointment);
            }

            appointment.Status = canonicalStatus;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Status updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reopening appointment {AppointmentId} failed.", id);
            return StatusCode(500, ApiResponse.Error("The appointment could not be reopened. Please try again."));
        }
    }

    private async Task RestoreStockFromTransaction(Transaction txn)
    {
        var now = DateTime.Now;
        var productIds = txn.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
        var lookup = products.ToDictionary(p => p.Id);

        var reversal = new Transaction
        {
            InvoiceNumber = $"RVT-{now:yyMMdd-HHss}-{InvoiceHelper.ShortCode()}",
            TransactionDate = now,
            TransactionType = TransactionTypes.StockCorrection,
            PaymentMethod = "N/A",
            LocationId = txn.LocationId,
            Remarks = $"Stock restored from voided sale {txn.InvoiceNumber}",
            TotalAmount = 0,
            IsVoided = false
        };

        foreach (var item in txn.Items)
        {
            if (lookup.TryGetValue(item.ProductId, out var product))
            {
                var stockBefore = product.CurrentStock;
                product.AddStock(item.Quantity);
                reversal.Items.Add(new TransactionItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    UnitCost = item.UnitCost,
                    Quantity = item.Quantity,
                    StockBefore = stockBefore,
                    StockAfter = product.CurrentStock,
                    LineTotal = 0
                });
            }
        }

        _context.Transactions.Add(reversal);
    }

    private async Task ReserveAppointmentProducts(Appointment appointment)
    {
        var productIds = ExtractProductIds(appointment.SelectedProductsJson);
        if (productIds.Count == 0) return;
        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
        var lookup = products.ToDictionary(p => p.Id);
        foreach (var id in productIds)
        {
            if (lookup.TryGetValue(id, out var product))
                product.ReserveStock(1);
        }
    }

    private async Task ReleaseAppointmentReservations(Appointment appointment)
    {
        var productIds = ExtractProductIds(appointment.SelectedProductsJson);
        if (productIds.Count == 0) return;
        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
        var lookup = products.ToDictionary(p => p.Id);
        foreach (var id in productIds)
        {
            if (lookup.TryGetValue(id, out var product) && product.ReservedStock > 0)
                product.ReleaseStock(1);
        }
    }

    private static List<int> ExtractProductIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var breakdown = JsonSerializer.Deserialize<List<ServiceProductBreakdown>>(json, JsonOpts);
            return breakdown?.SelectMany(s => s.Products.Where(p => p.Selected && p.Id > 0)).Select(p => p.Id).Distinct().ToList() ?? [];
        }
        catch { return []; }
    }

    [HttpPut("{id}/products")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> UpdateProducts(int id, [FromBody] UpdateAppointmentProductsDto dto)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null) return NotFound(ApiResponse.NotFound("Appointment"));
        if (appointment.Status is "Completed" or "Cancelled")
            return Conflict(ApiResponse.Error("Cannot edit parts for completed or cancelled appointments."));
        if (string.Equals(appointment.Category, "Build", StringComparison.OrdinalIgnoreCase))
            return Conflict(ApiResponse.Error("Build installation appointments do not support parts editing."));

        appointment.SelectedProductsJson = dto.SelectedProductsJson;
        appointment.TotalAmount = dto.TotalAmount;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Products updated" });
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
            var appointment = await _context.Appointments.FindAsync(id, cancellationToken);
            if (appointment == null) return NotFound(ApiResponse.NotFound("Appointment"));
            if (appointment.Status == WorkOrderStatuses.Confirmed)
                await ReleaseAppointmentReservations(appointment);

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
        catch (WorkOrderConflictException exception)
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

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelMyAppointment(int id)
    {
        var customer = await _userManager.GetUserAsync(User);
        if (customer is null) return Unauthorized();

        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment is null) return NotFound(ApiResponse.NotFound("Appointment"));
        if (appointment.CustomerEmail != customer.Email)
            return Forbid();

        if (appointment.Status != WorkOrderStatuses.Pending)
            return Conflict(ApiResponse.Error("Only pending appointments can be cancelled."));

        if (appointment.CreatedAt < DateTime.UtcNow.AddMinutes(-30))
            return Conflict(ApiResponse.Error("Appointments can only be cancelled within 30 minutes of booking."));

        appointment.Status = WorkOrderStatuses.Cancelled;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Appointment cancelled." });
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
        InvoiceNumber = a.Transaction?.InvoiceNumber,
        MotorcycleId = a.MotorcycleId,
        Motorcycle = MapMotorcycle(a.Motorcycle)
    };

    private static MotorcycleOptionDto? MapMotorcycle(Motorcycle? motorcycle) => motorcycle is null
        ? null
        : new MotorcycleOptionDto
        {
            Id = motorcycle.Id,
            Brand = motorcycle.Brand,
            Model = motorcycle.Model,
            BaseCC = motorcycle.BaseCC
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

    public class UpdateAppointmentProductsDto
    {
        public string SelectedProductsJson { get; set; } = "";
        public decimal TotalAmount { get; set; }
    }
}
