using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StockSense.Application.DTOs;
using StockSense.Application.Exceptions;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data.Repositories;
using StockSense.Infrastructure.Data;
using StockSense.Web.Options;
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
    private readonly PayMongoService _payMongo;
    private readonly PayMongoOptions _payMongoOptions;
    private readonly IAdminPinService? _adminPinService;
    private readonly IWorkOrderEmailSender _workOrderEmail;
    private static TimeZoneInfo PhZone
    {
        get
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"); }
            catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila"); }
        }
    }
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AppointmentsController(
        AppointmentRepository repo,
        StoreServiceRepository serviceRepo,
        IWorkOrderCheckoutService checkoutService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        MotorcycleRepository motorcycleRepository,
        ILogger<AppointmentsController> logger,
        PayMongoService payMongo,
        IOptions<PayMongoOptions> payMongoOptions,
        IWorkOrderEmailSender workOrderEmail,
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
        _payMongo = payMongo;
        _payMongoOptions = payMongoOptions.Value;
        _workOrderEmail = workOrderEmail;
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

            dto.TimeSlot = dto.TimeSlot?.Trim() ?? string.Empty;
            if (!TimeSpan.TryParseExact(dto.TimeSlot, @"hh\:mm", null, out var requestedStart) && !TimeSpan.TryParse(dto.TimeSlot, out requestedStart))
                return BadRequest(ApiResponse.Error("Time slot must be in HH:mm format (00:00-23:59)."));

            int totalDuration = matchedServices.Sum(s => s.EstimatedMinutes);
            // Build Installation has fixed 60 min and no StoreService entry — handle separately
            var isBuildCategory = string.Equals(dto.Category?.Trim(), "Build", StringComparison.OrdinalIgnoreCase);
            if (isBuildCategory && totalDuration == 0)
                totalDuration = 60;
            DateTime phNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhZone);
            var requestedEnd = requestedStart.Add(TimeSpan.FromMinutes(Math.Max(totalDuration, 15)));
            var existing = await _repo.GetAppointmentsByDateAndMechanicAsync(dto.AppointmentDate, null);
            var conflict = existing.Any(a =>
                TimeSpan.TryParse(a.TimeSlot?.Trim(), out var existingStart) &&
                (requestedStart < existingStart.Add(TimeSpan.FromMinutes(Math.Max(a.DurationMinutes, 15)))) &&
                (requestedEnd > existingStart));
            if (conflict)
                return Conflict(ApiResponse.Error("The selected time slot overlaps with an existing booking."));

            var appointment = new Appointment
            {
                CustomerName = customerFullName,
                CustomerEmail = customerEmail,
                CustomerUserId = customer.Id,
                ContactNumber = dto.ContactNumber,
                AppointmentDate = DateTime.SpecifyKind(dto.AppointmentDate.Date, DateTimeKind.Unspecified),
                TimeSlot = requestedStart.ToString(@"hh\:mm"),
                Category = string.IsNullOrWhiteSpace(dto.Category) ? "General Service" : dto.Category,
                ServicesRequested = flatServices,
                SelectedProductsJson = dto.SelectedProductsJson,
                SelectedServicesJson = dto.SelectedServicesJson,
                Status = customer.IsTrusted ? "Confirmed" : "Pending",
                CreatedAt = phNow,
                TotalAmount = isBuildCategory ? 0m : serviceTotal + productTotal,
                DurationMinutes = totalDuration,
                MechanicName = string.IsNullOrWhiteSpace(dto.MechanicName) ? "Any Available" : dto.MechanicName.Trim(),
                MotorcycleId = motorcycle?.Id
            };

            if (isBuildCategory)
            {
                var latestBuild = await _context.BuildRequests
                    .Where(b => b.CustomerUserId == customer.Id && b.Status == WorkOrderStatuses.Pending)
                    .OrderByDescending(b => b.CreatedAt)
                    .FirstOrDefaultAsync();
                if (latestBuild != null)
                {
                    appointment.BuildRequestId = latestBuild.Id;
                    appointment.DurationMinutes = 60;
                }
            }

            var saved = await _repo.AddAsync(appointment);
            return Ok(new { message = "Appointment booked successfully!", id = saved.Id, isTrusted = customer.IsTrusted });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Creating an appointment failed.");
            return StatusCode(500, ApiResponse.Error("The appointment could not be booked. Please try again."));
        }
    }

    [HttpPost("{id}/create-payment")]
    public async Task<IActionResult> CreatePayment(int id)
    {
        var customer = await _userManager.GetUserAsync(User);
        if (customer is null) return Unauthorized();

        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment is null) return NotFound(ApiResponse.NotFound("Appointment"));
        if (appointment.CustomerEmail != customer.Email) return Forbid();

        if (appointment.PaymentStatus == PaymentStatuses.Paid)
            return Ok(new { checkoutUrl = (string?)null, paymentStatus = PaymentStatuses.Paid });

        if (appointment.PaymentStatus == PaymentStatuses.AwaitingPayment
            && !string.IsNullOrEmpty(appointment.PaymentLinkId))
        {
            var (existingStatus, existingCheckoutUrl) = await _payMongo.GetLinkDetailsAsync(appointment.PaymentLinkId);
            if (existingStatus is not null)
            {
                var mapped = PayMongoService.MapLinkStatus(existingStatus);
                if (mapped != appointment.PaymentStatus)
                {
                    appointment.PaymentStatus = mapped;
                    await _context.SaveChangesAsync();
                }
                return Ok(new { checkoutUrl = existingCheckoutUrl, paymentStatus = appointment.PaymentStatus });
            }
        }

        var serviceNames = appointment.ServicesRequested
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        var services = await _serviceRepo.GetByNamesAsync(serviceNames);
        decimal serviceTotal = services.Sum(s => s.Price);

        int slotCount = (int)Math.Ceiling(appointment.DurationMinutes / 30.0);
        decimal reservationFee = 100m + Math.Max(0, slotCount - 2) * 25m;

        if (reservationFee <= 0)
        {
            appointment.PaymentStatus = PaymentStatuses.NotRequired;
            await _context.SaveChangesAsync();
            return Ok(new { checkoutUrl = (string?)null, paymentStatus = PaymentStatuses.NotRequired });
        }

        if (!_payMongoOptions.Enabled)
            return StatusCode(503, ApiResponse.Error("Online payments are currently unavailable. Please try again later."));

        try
        {
            var (linkId, checkoutUrl) = await _payMongo.CreatePaymentLinkAsync(
                reservationFee,
                $"Appointment #{id} reservation fee",
                $"appt-{id}-{DateTime.UtcNow:yyyyMMddHHmmssfff}");
            appointment.PaymentLinkId = linkId;
            appointment.PaymentStatus = PaymentStatuses.AwaitingPayment;
            appointment.PaymentAmount = reservationFee;
            await _context.SaveChangesAsync();
            return Ok(new { checkoutUrl, paymentStatus = PaymentStatuses.AwaitingPayment });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "PayMongo configuration error for appointment {AppointmentId}.", id);
            return StatusCode(502, ApiResponse.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Creating PayMongo payment link for appointment {AppointmentId} failed.", id);
            return StatusCode(502, ApiResponse.Error("The payment page could not be initialized. Please try again."));
        }
    }

    [HttpGet("{id}/payment-status")]
    public async Task<IActionResult> GetPaymentStatus(int id)
    {
        var customer = await _userManager.GetUserAsync(User);
        if (customer is null) return Unauthorized();

        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment is null) return NotFound(ApiResponse.NotFound("Appointment"));
        if (appointment.CustomerEmail != customer.Email) return Forbid();

        if (appointment.PaymentStatus is PaymentStatuses.Paid or PaymentStatuses.NotRequired)
            return Ok(new { paymentStatus = appointment.PaymentStatus });

        if (string.IsNullOrEmpty(appointment.PaymentLinkId))
            return Ok(new { paymentStatus = appointment.PaymentStatus });

        var status = await _payMongo.GetLinkStatusAsync(appointment.PaymentLinkId);
        var mapped = PayMongoService.MapLinkStatus(status);
        if (mapped != appointment.PaymentStatus)
        {
            appointment.PaymentStatus = mapped;
            if (mapped == PaymentStatuses.Paid && appointment.Status == "Pending")
                appointment.Status = "Confirmed";
            await _context.SaveChangesAsync();
        }

        return Ok(new { paymentStatus = appointment.PaymentStatus });
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PayMongoWebhook()
    {
        var payload = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signatureHeader = HttpContext.Request.Headers["PayMongo-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(_payMongoOptions.WebhookSecret))
        {
            _logger.LogWarning("PayMongo webhook received but WebhookSecret is not configured.");
            return Ok();
        }

        if (!PayMongoService.VerifyWebhookSignature(payload, signatureHeader, _payMongoOptions.WebhookSecret))
        {
            _logger.LogWarning("PayMongo webhook signature verification failed.");
            return Unauthorized();
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var eventType = document.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("type").GetString();

            if (eventType != "link.paid")
                return Ok();

            var linkId = document.RootElement.GetProperty("data").GetProperty("attributes")
                .GetProperty("data").GetProperty("id").GetString();

            if (string.IsNullOrEmpty(linkId))
                return Ok();

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.PaymentLinkId == linkId);

            if (appointment is null)
            {
                _logger.LogInformation("Webhook received for unknown link {LinkId}.", linkId);
                return Ok();
            }

            if (appointment.PaymentStatus != PaymentStatuses.Paid)
            {
                appointment.PaymentStatus = PaymentStatuses.Paid;
                if (appointment.Status == "Pending")
                    appointment.Status = "Confirmed";
                _context.WorkOrderAudits.Add(new WorkOrderAudit
                {
                    WorkOrderType = "Appointment",
                    WorkOrderId = appointment.Id,
                    Action = "PaymentConfirmed",
                    PreviousValue = appointment.PaymentStatus,
                    NewValue = PaymentStatuses.Paid,
                    ActorUserId = "system",
                    ActorRole = "System",
                    Reason = "PayMongo webhook",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
                _logger.LogInformation("Appointment {AppointmentId} payment confirmed via webhook.", appointment.Id);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing PayMongo webhook failed.");
            return Ok();
        }
    }

    [HttpGet("booked-slots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBookedSlots([FromQuery] DateTime date, [FromQuery] string? mechanic)
    {
        try
        {
            var appointments = await _repo.GetAppointmentsByDateAndMechanicAsync(date, mechanic);
            var slots = appointments.Select(a => new BookedSlotDto { TimeSlot = a.TimeSlot, EstimatedMinutes = a.DurationMinutes }).ToList();
            return Ok(slots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve booked slots for {Date} and mechanic {Mechanic}.", date, mechanic);
            return StatusCode(500, ApiResponse.Error("Could not retrieve booked slots. Please try again."));
        }
    }

    [HttpGet("all")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<ActionResult<List<AppointmentDto>>> GetAllAppointments()
    {
        try
        {
            await ExpirePastDueAppointmentsAsync();
            var appointments = await _repo.GetAllAsync();
            await EnrichCustomerIdentitiesAsync(appointments);
            var dtos = appointments.Select(a => MapToDto(a)).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all appointments.");
            return StatusCode(500, ApiResponse.Error("Could not retrieve appointments. Please try again."));
        }
    }

    [HttpPut("{id}/assign-mechanic")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> AssignMechanic(int id, [FromBody] MechanicAssignmentDto assignment)
    {
        try
        {
            var appointment = await _repo.GetByIdAsync(id);
            if (appointment == null) return NotFound(ApiResponse.NotFound("Appointment"));

            if (appointment.Status is WorkOrderStatuses.Completed or WorkOrderStatuses.Cancelled or WorkOrderStatuses.Expired)
                return Conflict(ApiResponse.Error("Completed, cancelled, and expired appointments are read-only."));
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Assigning mechanic for appointment {AppointmentId} failed.", id);
            return StatusCode(500, ApiResponse.Error("The mechanic could not be assigned. Please try again."));
        }
    }

    [HttpPut("{id}/details")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> UpdateDetails(int id, [FromBody] UpdateAppointmentDetailsDto dto)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null) return NotFound(ApiResponse.NotFound("Appointment"));

        if (appointment.Status is WorkOrderStatuses.Completed or WorkOrderStatuses.Cancelled or WorkOrderStatuses.Expired)
            return Conflict(ApiResponse.Error("Completed, cancelled, and expired appointments are read-only."));

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
        if (reason?.Length > 500)
            return BadRequest(ApiResponse.Error("The reason cannot exceed 500 characters."));

        var timeSlot = dto.TimeSlot?.Trim() ?? string.Empty;
        if (!TimeSpan.TryParseExact(timeSlot, @"hh\:mm", null, out var requestedStart) && !TimeSpan.TryParse(timeSlot, out requestedStart))
            return BadRequest(ApiResponse.Error("Time slot must be in HH:mm format (00:00-23:59)."));

        var duration = dto.DurationMinutes > 0 ? dto.DurationMinutes : appointment.DurationMinutes;
        var requestedEnd = requestedStart.Add(TimeSpan.FromMinutes(Math.Max(duration, 15)));

        var existing = await _repo.GetAppointmentsByDateAndMechanicAsync(dto.AppointmentDate, null);
        var conflict = existing.Any(a =>
            a.Id != id &&
            TimeSpan.TryParse(a.TimeSlot?.Trim(), out var existingStart) &&
            requestedStart < existingStart.Add(TimeSpan.FromMinutes(Math.Max(a.DurationMinutes, 15))) &&
            requestedEnd > existingStart);
        if (conflict)
            return Conflict(ApiResponse.Error("The selected time slot overlaps with an existing booking."));

        var previousSchedule = $"{appointment.AppointmentDate:yyyy-MM-dd} {appointment.TimeSlot}";
        var newSchedule = $"{dto.AppointmentDate:yyyy-MM-dd} {requestedStart:hh\\:mm}";
        var previousMechanic = appointment.MechanicName;
        var newMechanic = string.IsNullOrWhiteSpace(dto.MechanicName) ? "Any Available" : dto.MechanicName.Trim();

        appointment.AppointmentDate = DateTime.SpecifyKind(dto.AppointmentDate.Date, DateTimeKind.Unspecified);
        appointment.TimeSlot = requestedStart.ToString(@"hh\:mm");
        appointment.DurationMinutes = duration;
        appointment.MechanicName = newMechanic;

        if (!string.Equals(previousSchedule, newSchedule, StringComparison.Ordinal))
            AddAudit("Appointment", id, "ScheduleChanged", previousSchedule, newSchedule, reason, approval);
        if (!string.Equals(previousMechanic, newMechanic, StringComparison.Ordinal))
            AddAudit("Appointment", id, "MechanicAssigned", previousMechanic, newMechanic, reason, approval);

        await _context.SaveChangesAsync();

        if (!string.Equals(previousSchedule, newSchedule, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(appointment.CustomerEmail))
        {
            _ = _workOrderEmail.SendRescheduleEmailAsync(
                appointment.CustomerEmail, appointment.CustomerName, id,
                previousSchedule, newSchedule, newMechanic);
        }

        return Ok(new { message = "Appointment details updated." });
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

            if (canonicalStatus == WorkOrderStatuses.Confirmed && !string.IsNullOrWhiteSpace(appointment.CustomerEmail))
            {
                var summary = $"Service: {appointment.ServicesRequested}\nDate: {appointment.AppointmentDate:MMMM dd, yyyy} at {appointment.TimeSlot}\nMechanic: {appointment.MechanicName}\nTotal: ₱{appointment.TotalAmount:N2}";
                _ = _workOrderEmail.SendStatusEmailAsync(appointment.CustomerEmail, appointment.CustomerName, "Appointment", "Confirmed", id, summary);
            }

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

        return null;
    }

    private async Task ReleaseAppointmentReservations(Appointment appointment)
    {
        await Task.CompletedTask;
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
        if (appointment.Status is "Completed" or "Cancelled" or "Expired")
            return Conflict(ApiResponse.Error("Cannot edit parts for completed, cancelled, or expired appointments."));
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
        decimal serviceTotal;
        if (!string.IsNullOrWhiteSpace(appointment.SelectedServicesJson))
        {
            try
            {
                var svcList = JsonSerializer.Deserialize<List<ServiceItem>>(appointment.SelectedServicesJson, JsonOpts) ?? [];
                serviceTotal = svcList.Sum(s => s.Price);
            }
            catch (JsonException)
            {
                var services = await _serviceRepo.GetByNamesAsync(serviceNames);
                serviceTotal = services.Sum(s => s.Price);
            }
        }
        else
        {
            var services = await _serviceRepo.GetByNamesAsync(serviceNames);
            serviceTotal = services.Sum(s => s.Price);
        }
        appointment.SelectedProductsJson = JsonSerializer.Serialize(breakdown);
        appointment.TotalAmount = serviceTotal + selectedIds.Sum(id => products[id].Price);
        AddAudit("Appointment", id, "ProductsChanged", null, string.Join(',', selectedIds), reason, approval);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Products updated" });
    }

    [HttpPut("{id}/services")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> UpdateServices(int id, [FromBody] UpdateAppointmentServicesDto dto)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null) return NotFound(ApiResponse.NotFound("Appointment"));
        if (appointment.Status is "Completed" or "Cancelled" or "Expired")
            return Conflict(ApiResponse.Error("Cannot edit services for completed, cancelled, or expired appointments."));
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

        List<ServiceItem> services;
        try
        {
            services = JsonSerializer.Deserialize<List<ServiceItem>>(dto.SelectedServicesJson, JsonOpts) ?? [];
        }
        catch (JsonException)
        {
            return BadRequest(ApiResponse.Error("The selected services are invalid."));
        }
        if (services.Count == 0)
            return BadRequest(ApiResponse.Error("At least one service is required."));

        var productTotal = 0m;
        if (!string.IsNullOrWhiteSpace(appointment.SelectedProductsJson))
        {
            try
            {
                var breakdown = JsonSerializer.Deserialize<List<ServiceProductBreakdown>>(appointment.SelectedProductsJson, JsonOpts) ?? [];
                productTotal = breakdown.SelectMany(item => item.Products).Where(p => p.Selected).Sum(p => p.Price);
            }
            catch (JsonException) { }
        }

        var previousServices = appointment.ServicesRequested;
        appointment.SelectedServicesJson = JsonSerializer.Serialize(services, JsonOpts);
        appointment.ServicesRequested = string.Join(", ", services.Select(s => s.Name));
        appointment.TotalAmount = services.Sum(s => s.Price) + productTotal;
        AddAudit("Appointment", id, "ServicesChanged", previousServices, appointment.ServicesRequested, reason, approval);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Services updated", totalAmount = appointment.TotalAmount });
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

            if (appointment.PaymentStatus == PaymentStatuses.Paid
                && string.Equals(request.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Completing appointment {AppointmentId} with Cash but payment was already received via PayMongo.",
                    id);
            }

            if (appointment.Status == WorkOrderStatuses.Confirmed)
                await ReleaseAppointmentReservations(appointment);

            var receipt = await _checkoutService.CompleteAppointmentAsync(
                id,
                request,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                InventoryDefaults.LocationId,
                cancellationToken);

            if (appointment.PaymentStatus != PaymentStatuses.Paid
                && appointment.PaymentStatus != PaymentStatuses.NotRequired)
            {
                appointment.PaymentStatus = PaymentStatuses.Paid;
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(appointment.CustomerEmail))
            {
                receipt.ServiceDescription = appointment.ServicesRequested;
                var summary = $"Service: {appointment.ServicesRequested}\nDate: {appointment.AppointmentDate:MMMM dd, yyyy}\nTotal: ₱{receipt.TotalAmount:N2}\nPayment: {receipt.PaymentMethod}";
                _ = _workOrderEmail.SendStatusEmailAsync(appointment.CustomerEmail, appointment.CustomerName, "Appointment", "Completed", id, summary, receipt);
            }

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
        await ExpirePastDueAppointmentsAsync();
        var appointments = await _repo.GetByCustomerIdentityAsync(
            customer.Id,
            customer.Email ?? string.Empty,
            GetFullName(customer));
        await EnrichCustomerIdentitiesAsync(appointments);
        var dtos = appointments.Select(a => MapToDto(a)).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id}/audit")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<ActionResult<List<WorkOrderAuditDto>>> GetAuditTrail(int id)
    {
        try
        {
            var audits = await _context.WorkOrderAudits
                .AsNoTracking()
                .Where(audit => audit.WorkOrderType == "Appointment" && audit.WorkOrderId == id)
                .OrderByDescending(audit => audit.CreatedAt)
                .ToListAsync();
            var actorIds = audits
                .Select(audit => audit.ActorUserId)
                .Where(userId => !string.IsNullOrEmpty(userId))
                .Distinct()
                .ToList();
            var users = await _userManager.Users.AsNoTracking()
                .Where(user => actorIds.Contains(user.Id))
                .ToListAsync();
            var actorNames = users.ToDictionary(user => user.Id, GetFullName);
            return Ok(audits.Select(audit => MapAudit(audit, actorNames)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit trail for appointment {AppointmentId}.", id);
            return StatusCode(500, ApiResponse.Error("Could not retrieve the appointment history. Please try again."));
        }
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
        SelectedServicesJson = a.SelectedServicesJson,
        TotalAmount = a.TotalAmount, Status = a.Status, Category = a.Category,
        MechanicName = a.MechanicName, DurationMinutes = a.DurationMinutes,
        CompletedAt = a.CompletedAt, TransactionId = a.TransactionId,
        InvoiceNumber = a.Transaction?.InvoiceNumber,
        MotorcycleId = a.MotorcycleId,
        Motorcycle = MapMotorcycle(a.Motorcycle),
        PaymentLinkId = a.PaymentLinkId,
        PaymentStatus = a.PaymentStatus,
        PaymentAmount = a.PaymentAmount
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

    private async Task ExpirePastDueAppointmentsAsync()
    {
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhZone).Date;
        var pastDue = await _context.Appointments
            .Where(appointment =>
                (appointment.Status == WorkOrderStatuses.Pending || appointment.Status == WorkOrderStatuses.Confirmed)
                && appointment.AppointmentDate < today
                && appointment.PaymentStatus != PaymentStatuses.Paid)
            .ToListAsync();

        foreach (var appointment in pastDue)
        {
            var previousStatus = appointment.Status;
            appointment.Status = WorkOrderStatuses.Expired;
            if (appointment.PaymentStatus == PaymentStatuses.AwaitingPayment)
                appointment.PaymentStatus = PaymentStatuses.Expired;
            _context.WorkOrderAudits.Add(new WorkOrderAudit
            {
                WorkOrderType = "Appointment",
                WorkOrderId = appointment.Id,
                Action = "StatusChanged",
                PreviousValue = previousStatus,
                NewValue = WorkOrderStatuses.Expired,
                ActorUserId = "system",
                ActorRole = "System",
                Reason = "Scheduled date has passed",
                CreatedAt = DateTime.Now
            });
        }

        if (pastDue.Count > 0)
            await _context.SaveChangesAsync();
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

    private static WorkOrderAuditDto MapAudit(WorkOrderAudit audit, IReadOnlyDictionary<string, string> actorNames) => new()
    {
        Id = audit.Id,
        WorkOrderType = audit.WorkOrderType,
        WorkOrderId = audit.WorkOrderId,
        Action = audit.Action,
        PreviousValue = audit.PreviousValue,
        NewValue = audit.NewValue,
        ActorUserId = audit.ActorUserId,
        ActorName = actorNames.GetValueOrDefault(audit.ActorUserId, string.Empty),
        ActorRole = audit.ActorRole,
        ApproverUserId = audit.ApproverUserId,
        ApproverEmail = audit.ApproverEmail,
        Reason = audit.Reason,
        CreatedAt = audit.CreatedAt
    };

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

    private class ServiceItem
    {
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }

    public class UpdateAppointmentServicesDto
    {
        public string SelectedServicesJson { get; set; } = "";
        public string? AdminUserId { get; set; }
        public string? AdminEmail { get; set; }
        public string? AdminPin { get; set; }
        public string? Reason { get; set; }
    }
}
