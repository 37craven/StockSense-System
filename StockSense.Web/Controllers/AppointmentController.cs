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
using StockSense.Web.Services;

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
    private readonly IAdminPinService? _adminPinService;
    private static readonly TimeZoneInfo PhZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AppointmentsController(
        AppointmentRepository repo,
        StoreServiceRepository serviceRepo,
        IWorkOrderCheckoutService checkoutService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        MotorcycleRepository motorcycleRepository,
        ILogger<AppointmentsController> logger,
        IAdminPinService? adminPinService = null)
    {
        _repo = repo;
        _serviceRepo = serviceRepo;
        _checkoutService = checkoutService;
        _userManager = userManager;
        _context = context;
        _adminPinService = adminPinService;
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
            List<int> selectedProductIds = [];

            if (!string.IsNullOrWhiteSpace(dto.SelectedProductsJson))
            {
                List<ServiceProductBreakdown>? breakdown;
                try
                {
                    breakdown = JsonSerializer.Deserialize<List<ServiceProductBreakdown>>(dto.SelectedProductsJson, JsonOpts);
                }
                catch (JsonException)
                {
                    return BadRequest(ApiResponse.Error("The selected parts are invalid. Refresh the booking page and try again."));
                }
                if (breakdown != null)
                {
                    productTotal = breakdown.Sum(s => s.Products.Where(p => p.Selected).Sum(p => p.Price));
                    selectedProductIds = breakdown
                        .SelectMany(service => service.Products)
                        .Where(product => product.Selected && product.Id > 0)
                        .Select(product => product.Id)
                        .ToList();
                }
            }

            var stockError = await StockAvailabilityValidator.ValidateAsync(
                _context, selectedProductIds, "appointment", HttpContext.RequestAborted);
            if (stockError is not null)
                return Conflict(ApiResponse.Error(stockError));

            int totalDuration = matchedServices.Sum(s => s.EstimatedMinutes);
            DateTime phNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhZone);

            if (TimeSpan.TryParse(dto.TimeSlot, out var requestedStart))
            {
                var requestedEnd = requestedStart.Add(TimeSpan.FromMinutes(totalDuration));
                var existing = await _repo.GetAppointmentsByDateAndMechanicAsync(dto.AppointmentDate, null);
                var conflict = existing.Any(a =>
                    TimeSpan.TryParse(a.TimeSlot, out var existingStart) &&
                    (requestedStart < existingStart.Add(TimeSpan.FromMinutes(Math.Max(a.DurationMinutes, 15)))) &&
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

        if (appointment.Status is WorkOrderStatuses.Completed or WorkOrderStatuses.Cancelled)
            return Conflict(ApiResponse.Error("Completed and cancelled appointments are read-only."));
        if (appointment.Status == WorkOrderStatuses.Pending)
        {
            var transitionError = WorkOrderRules.ValidateStatusTransition(
                appointment.Status,
                WorkOrderStatuses.Confirmed,
                User.IsInRole("Admin"));
            if (transitionError is not null) return Conflict(ApiResponse.Error(transitionError));
        }

        var previousMechanic = appointment.MechanicName;
        appointment.MechanicName = assignment.MechanicName;
        appointment.DurationMinutes = assignment.DurationMinutes;
        appointment.Status = WorkOrderStatuses.Confirmed;
        AddAudit("Appointment", id, "MechanicAssigned", previousMechanic, assignment.MechanicName, null);
        await _repo.UpdateAsync(appointment);
        return Ok(new { message = $"Assigned to {assignment.MechanicName}" });
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateWorkOrderStatusDto request)
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

        if (string.Equals(request.Status, WorkOrderStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Error("Use the completion checkout endpoint to complete an appointment."));

        var canonicalStatus = new[] { WorkOrderStatuses.Pending, WorkOrderStatuses.Confirmed, WorkOrderStatuses.Cancelled }
            .SingleOrDefault(value => string.Equals(value, request.Status?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (canonicalStatus is null) return BadRequest(ApiResponse.Error("Unsupported appointment status."));
        var isAdmin = User.IsInRole("Admin");
        AdminPinVerificationResult? approval = null;
        if (!isAdmin && appointment.Status != WorkOrderStatuses.Pending)
        {
            if (_adminPinService is null) return StatusCode(403, ApiResponse.Error("Admin approval is required."));
            approval = !string.IsNullOrWhiteSpace(request.AdminUserId)
                ? await _adminPinService.VerifyByUserIdAsync(request.AdminUserId, request.AdminPin ?? "")
                : await _adminPinService.VerifyAsync(request.AdminEmail ?? "", request.AdminPin ?? "");
            if (!approval.Succeeded)
                return StatusCode(approval.LockedUntil.HasValue ? 429 : 403, ApiResponse.Error(approval.Error ?? "Admin approval failed."));
        }
        var transitionError = WorkOrderRules.ValidateStatusTransition(appointment.Status, canonicalStatus, isAdmin || approval?.Succeeded == true);
        if (transitionError is not null) return Conflict(ApiResponse.Error(transitionError));
        var reason = request.Reason?.Trim();
        if (WorkOrderRules.RequiresAdminReason(appointment.Status, canonicalStatus) && string.IsNullOrWhiteSpace(reason))
            return BadRequest(ApiResponse.Error("A reason is required for this admin action."));
        if (reason?.Length > 500)
            return BadRequest(ApiResponse.Error("The reason cannot exceed 500 characters."));

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
                var reservationError = await TryReserveAppointmentProducts(appointment);
                if (reservationError is not null)
                    return Conflict(ApiResponse.Error(reservationError));
            }

            var previousStatus = appointment.Status;
            appointment.Status = canonicalStatus;
            AddAudit("Appointment", id, "StatusChanged", previousStatus, canonicalStatus, reason, approval);
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

    private async Task<string?> TryReserveAppointmentProducts(Appointment appointment)
    {
        var productIds = ExtractProductIds(appointment.SelectedProductsJson);
        if (productIds.Count == 0) return null;
        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
        var lookup = products.ToDictionary(p => p.Id);

        // Validate the complete selection before changing any reservation. This ensures
        // one unavailable part cannot leave earlier parts partially reserved.
        var unavailable = productIds
            .Where(id => !lookup.TryGetValue(id, out var product) || !product.IsActive || product.AvailableStock < 1)
            .Select(id => lookup.TryGetValue(id, out var product)
                ? product.Name
                : $"part #{id} (no longer available)")
            .ToList();
        if (unavailable.Count > 0)
            return $"This appointment cannot be confirmed because there is not enough stock for: {string.Join(", ", unavailable)}. Please update the selected parts or restock them first.";

        foreach (var id in productIds)
            lookup[id].ReserveStock(1);

        return null;
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
        AdminPinVerificationResult? approval = null;
        if (appointment.Status != WorkOrderStatuses.Pending && !User.IsInRole("Admin"))
        {
            if (_adminPinService is null) return StatusCode(403, ApiResponse.Error("Admin approval is required."));
            approval = !string.IsNullOrWhiteSpace(dto.AdminUserId)
                ? await _adminPinService.VerifyByUserIdAsync(dto.AdminUserId, dto.AdminPin ?? "")
                : await _adminPinService.VerifyAsync(dto.AdminEmail ?? "", dto.AdminPin ?? "");
            if (!approval.Succeeded)
                return StatusCode(approval.LockedUntil.HasValue ? 429 : 403, ApiResponse.Error(approval.Error ?? "Admin approval failed."));
        }
        var reason = dto.Reason?.Trim();
        if (appointment.Status != WorkOrderStatuses.Pending && string.IsNullOrWhiteSpace(reason))
            return BadRequest(ApiResponse.Error("Please provide a reason for this change."));
        if (string.Equals(appointment.Category, "Build", StringComparison.OrdinalIgnoreCase))
            return Conflict(ApiResponse.Error("Build installation appointments do not support parts editing."));

        List<ServiceProductBreakdown> breakdown;
        try
        {
            breakdown = JsonSerializer.Deserialize<List<ServiceProductBreakdown>>(dto.SelectedProductsJson, JsonOpts) ?? [];
        }
        catch (JsonException)
        {
            return BadRequest(ApiResponse.Error("The selected products are invalid."));
        }
        var selectedIds = breakdown.SelectMany(item => item.Products).Where(product => product.Selected && product.Id > 0).Select(product => product.Id).ToList();
        var products = await _context.Products.Where(product => selectedIds.Contains(product.Id) && product.IsActive).ToDictionaryAsync(product => product.Id);
        if (products.Count != selectedIds.Distinct().Count())
            return BadRequest(ApiResponse.Error("One or more selected products are unavailable."));
        foreach (var product in breakdown.SelectMany(item => item.Products).Where(product => product.Selected && product.Id > 0))
        {
            product.Name = products[product.Id].Name;
            product.Price = products[product.Id].Price;
        }
        var serviceNames = appointment.ServicesRequested.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var services = await _serviceRepo.GetByNamesAsync(serviceNames);
        appointment.SelectedProductsJson = JsonSerializer.Serialize(breakdown);
        appointment.TotalAmount = services.Sum(service => service.Price) + selectedIds.Sum(id => products[id].Price);
        AddAudit("Appointment", id, "ProductsChanged", null, string.Join(',', selectedIds), reason, approval);
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

    private void AddAudit(string type, int id, string action, string? previousValue, string? newValue, string? reason,
        AdminPinVerificationResult? approval = null)
    {
        _context.WorkOrderAudits.Add(new WorkOrderAudit
        {
            WorkOrderType = type,
            WorkOrderId = id,
            Action = action,
            PreviousValue = previousValue,
            NewValue = newValue,
            ActorUserId = HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown",
            ActorRole = HttpContext?.User.IsInRole("Admin") == true ? "Admin" : "Employee",
            ApproverUserId = approval?.AdminUserId,
            ApproverEmail = approval?.AdminEmail,
            Reason = reason,
            CreatedAt = DateTime.Now
        });
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
        public string? AdminUserId { get; set; }
        public string? AdminEmail { get; set; }
        public string? AdminPin { get; set; }
        public string? Reason { get; set; }
    }
}
