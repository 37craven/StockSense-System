using System.Security.Claims;
using System.Collections.Concurrent;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Services;
using StockSense.Web.Helpers;

namespace StockSense.Web.Controllers;

[ApiController]
[Route("api/order-slips")]
[Authorize(Roles = "Admin, Employee")]
public sealed class OrderSlipsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IOrderSlipWorkflowService _workflow;
    private readonly DocumentService _docService;
    private readonly PdfDownloadCache _pdfCache;
    private readonly OrderEmailSender _orderEmailSender;
    private readonly ILogger<OrderSlipsController> _logger;
    private readonly IAdminPinService? _adminPinService;
    // Prevents duplicate dispatches from repeated clicks within this application instance.
    // The rowversion/status checks remain the cross-instance safety boundary.
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> DispatchLocks = new();

    public OrderSlipsController(ApplicationDbContext context, IOrderSlipWorkflowService workflow,
        DocumentService docService, PdfDownloadCache pdfCache, OrderEmailSender orderEmailSender,
        ILogger<OrderSlipsController> logger, IAdminPinService? adminPinService = null)
    {
        _context = context;
        _workflow = workflow;
        _docService = docService;
        _pdfCache = pdfCache;
        _orderEmailSender = orderEmailSender;
        _logger = logger;
        _adminPinService = adminPinService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderSlipDto>>> GetAll(CancellationToken cancellationToken)
    {
        var slips = await _context.OrderSlips.AsNoTracking()
            .Include(x => x.Supplier).Include(x => x.Items)
            .OrderByDescending(x => x.GeneratedAt).ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
        return Ok(slips.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderSlipDto>> Get(int id, CancellationToken cancellationToken)
    {
        var slip = await _context.OrderSlips.AsNoTracking()
            .Include(x => x.Supplier).Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return slip is null ? NotFound() : Ok(ToDto(slip));
    }

    [HttpGet("{id:int}/download-pdf")]
    public async Task<IActionResult> DownloadPdf(int id, CancellationToken cancellationToken)
    {
        var slip = await _context.OrderSlips.AsNoTracking()
            .Include(x => x.Supplier).Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (slip is null) return NotFound();
        var bytes = _docService.GenerateOrderSlipPdf(ToDto(slip));
        var token = _pdfCache.Store(bytes);
        return Ok(new { token });
    }

    [HttpGet("preview")]
    public async Task<IActionResult> Preview(CancellationToken cancellationToken) =>
        ToActionResult(await _workflow.PreviewAsync(InventoryDefaults.LocationId, cancellationToken));

    [HttpPost("drafts")]
    public async Task<IActionResult> CreateDrafts(CreateOrderSlipDraftsCommand command, CancellationToken cancellationToken)
    {
        command.LocationId = InventoryDefaults.LocationId;
        command.CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ToActionResult(await _workflow.CreateDraftsAsync(command, cancellationToken));
    }

    [HttpGet("manual-catalog")]
    public async Task<IActionResult> GetManualCatalog(CancellationToken cancellationToken) =>
        ToActionResult(await _workflow.GetManualCatalogAsync(InventoryDefaults.LocationId, cancellationToken));

    [HttpPost("manual-draft")]
    public async Task<IActionResult> CreateManualDraft(
        CreateManualOrderSlipDraftCommand command, CancellationToken cancellationToken)
    {
        command.LocationId = InventoryDefaults.LocationId;
        command.CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ToActionResult(await _workflow.CreateManualDraftAsync(command, cancellationToken));
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(
        int id, OrderSlipTransitionCommand command, CancellationToken cancellationToken)
    {
        Prepare(command, id, OrderSlipStatuses.Approved);
        return ToActionResult(await _workflow.ApproveAsync(command, cancellationToken));
    }

    [HttpPost("{id:int}/mark-ordered")]
    public IActionResult MarkOrdered(
        int id, OrderSlipTransitionCommand command, CancellationToken cancellationToken)
    {
        return BadRequest(new
        {
            error = "Send the approved order to the supplier before marking it as ordered.",
            code = "SUPPLIER_EMAIL_REQUIRED",
            nextAction = $"/api/order-slips/{id}/send-to-supplier"
        });
    }

    [HttpPost("{id:int}/send-to-supplier")]
    public async Task<IActionResult> SendToSupplier(
        int id, OrderSlipTransitionCommand command, CancellationToken cancellationToken)
    {
        Prepare(command, id, OrderSlipStatuses.Ordered);
        var dispatchLock = DispatchLocks.GetOrAdd(id, static _ => new SemaphoreSlim(1, 1));
        await dispatchLock.WaitAsync(cancellationToken);
        try
        {
            _context.ChangeTracker.Clear();
            var slip = await _context.OrderSlips.AsNoTracking()
                .Include(value => value.Supplier)
                .Include(value => value.Items)
                .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
            if (slip is null)
                return NotFound(new { error = "Order slip was not found.", code = "NOT_FOUND" });
            if (slip.Status != OrderSlipStatuses.Approved)
                return BadRequest(new
                {
                    error = "Only an approved order can be sent to its supplier.",
                    code = "INVALID_STATUS"
                });
            if (command.RowVersion.Length == 0 || !command.RowVersion.SequenceEqual(slip.RowVersion))
                return Conflict(new { error = "This order changed. Reload it and try again.", code = "CONCURRENCY_CONFLICT" });
            if (slip.Supplier is null || string.IsNullOrWhiteSpace(slip.Supplier.Email)
                || !IsValidEmail(slip.Supplier.Email))
                return BadRequest(new
                {
                    error = "This supplier does not have a valid email address. Update the supplier first.",
                    code = "INVALID_SUPPLIER_EMAIL"
                });

            var dto = ToDto(slip);
            var pdf = _docService.GenerateOrderSlipPdf(dto);
            var slipNumber = string.IsNullOrWhiteSpace(dto.OrderSlipNumber) ? dto.SlipNumber : dto.OrderSlipNumber;
            var subject = $"Purchase Order - {slipNumber}";
            var body = PurchaseOrderEmailTemplate.Build(slipNumber);
            try
            {
                await _orderEmailSender.SendEmailWithAttachmentAsync(
                    slip.Supplier.Email.Trim(), subject, body, pdf, $"Order_{slipNumber}.pdf", cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Sending order slip {OrderSlipId} to its supplier failed.", id);
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = "The order email could not be sent. Check the supplier email or mail settings, then try again.",
                    code = "EMAIL_SEND_FAILED"
                });
            }

            command.Remarks = string.IsNullOrWhiteSpace(command.Remarks)
                ? $"Sent to supplier {slip.Supplier.Name}."
                : command.Remarks;
            var transition = await _workflow.MarkOrderedAsync(command, cancellationToken);
            if (!transition.IsSuccess)
            {
                _logger.LogError(
                    "Order slip {OrderSlipId} email was sent, but its Ordered transition failed with {ErrorCode}.",
                    id, transition.ErrorCode);
                return Conflict(new
                {
                    error = "The email was sent, but the order status could not be updated. Reload the order before taking another action.",
                    code = "EMAIL_SENT_STATUS_CONFLICT"
                });
            }
            return Ok(transition.Value);
        }
        finally
        {
            dispatchLock.Release();
        }
    }

    [HttpPost("{id:int}/close-remaining")]
    public async Task<IActionResult> CloseRemaining(
        int id, CloseOrderSlipShortCommand command, CancellationToken cancellationToken)
    {
        command.OrderSlipId = id;
        command.ActingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(command.ActingUserId))
            return Unauthorized(new { error = "Please sign in again before closing this order.", code = "USER_ID_REQUIRED" });
        command.ActorRole = User.IsInRole("Admin") ? "Admin" : "Employee";
        command.ApproverUserId = null;
        command.ApproverEmail = null;

        if (string.IsNullOrWhiteSpace(command.Reason))
            return BadRequest(new
            {
                error = "Please enter a reason for closing the remaining order.",
                code = "CLOSE_REASON_REQUIRED"
            });
        if (command.Reason.Trim().Length > 500)
            return BadRequest(new
            {
                error = "The reason cannot exceed 500 characters.",
                code = "CLOSE_REASON_TOO_LONG"
            });

        if (!User.IsInRole("Admin"))
        {
            if (_adminPinService is null)
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    error = "Admin approval is required to close the remaining order.",
                    code = "ADMIN_APPROVAL_REQUIRED"
                });

            var approval = await _adminPinService.VerifyByUserIdAsync(
                command.AdminUserId ?? string.Empty,
                command.AdminPin ?? string.Empty,
                cancellationToken);
            if (!approval.Succeeded)
                return StatusCode(
                    approval.LockedUntil.HasValue
                        ? StatusCodes.Status429TooManyRequests
                        : StatusCodes.Status403Forbidden,
                    new
                    {
                        error = approval.Error ?? "Admin approval failed.",
                        code = "ADMIN_APPROVAL_FAILED"
                    });

            command.ApproverUserId = approval.AdminUserId;
            command.ApproverEmail = approval.AdminEmail;
        }

        command.Reason = command.Reason.Trim();
        command.AdminPin = null;
        return ToActionResult(await _workflow.CloseShortAsync(command, cancellationToken));
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Cancel(int id, CancelOrderSlipCommand command, CancellationToken cancellationToken)
    {
        command.OrderSlipId = id;
        command.ActingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ToActionResult(await _workflow.CancelAsync(command, cancellationToken));
    }

    [HttpPost("{id:int}/receive")]
    public async Task<IActionResult> Receive(int id, ReceiveOrderSlipCommand command, CancellationToken cancellationToken)
    {
        command.OrderSlipId = id;
        command.LocationId = InventoryDefaults.LocationId;
        command.ReceivedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ToActionResult(await _workflow.ReceiveAsync(command, cancellationToken));
    }

    private void Prepare(OrderSlipTransitionCommand command, int id, string status)
    {
        command.OrderSlipId = id;
        command.TargetStatus = status;
        command.ActingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static bool IsValidEmail(string value)
    {
        try { return new MailAddress(value.Trim()).Address == value.Trim(); }
        catch (FormatException) { return false; }
    }

    private IActionResult ToActionResult<T>(OperationResult<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        var body = new { error = result.ErrorMessage, code = result.ErrorCode };
        if (result.IsConcurrencyConflict) return Conflict(body);
        return result.ErrorCode == "NOT_FOUND" ? NotFound(body) : BadRequest(body);
    }

    private static OrderSlipDto ToDto(OrderSlip slip) => new()
    {
        Id = slip.Id,
        SlipNumber = string.IsNullOrWhiteSpace(slip.SlipNumber) ? slip.OrderSlipNumber : slip.SlipNumber,
        OrderSlipNumber = string.IsNullOrWhiteSpace(slip.OrderSlipNumber) ? slip.SlipNumber : slip.OrderSlipNumber,
        DateGenerated = slip.DateGenerated,
        GeneratedAt = slip.GeneratedAt == default ? slip.DateGenerated : slip.GeneratedAt,
        SupplierId = slip.SupplierId,
        SupplierName = slip.Supplier?.Name ?? "Unknown supplier",
        SupplierEmail = slip.Supplier?.Email ?? string.Empty,
        IsReceived = slip.IsReceived,
        LocationId = slip.LocationId,
        Status = string.IsNullOrWhiteSpace(slip.Status) ? (slip.IsReceived ? OrderSlipStatuses.Completed : OrderSlipStatuses.Draft) : slip.Status,
        ApprovedAt = slip.ApprovedAt,
        OrderedAt = slip.OrderedAt,
        ExpectedDeliveryDate = slip.ExpectedDeliveryDate,
        CompletedAt = slip.CompletedAt,
        CreatedByUserId = slip.CreatedByUserId,
        ApprovedByUserId = slip.ApprovedByUserId,
        TotalEstimatedCost = slip.TotalEstimatedCost,
        Remarks = slip.Remarks,
        RowVersion = slip.RowVersion,
        Items = slip.Items.Select(item => new OrderSlipItemDto
        {
            Id = item.Id, ProductId = item.ProductId, ProductName = item.ProductName,
            Brand = item.Brand, Category = item.Category ?? string.Empty,
            CurrentStock = item.CurrentStock, ReorderTarget = item.ReorderTarget,
            Quantity = item.Quantity, ReceivedQuantity = item.ReceivedQuantity,
            CurrentStockSnapshot = item.CurrentStockSnapshot,
            IncomingStockSnapshot = item.IncomingStockSnapshot,
            ReservedStockSnapshot = item.ReservedStockSnapshot,
            InventoryPositionSnapshot = item.InventoryPositionSnapshot,
            AverageDailyDemandSnapshot = item.AverageDailyDemandSnapshot,
            LeadTimeDaysSnapshot = item.LeadTimeDaysSnapshot,
            SafetyStockSnapshot = item.SafetyStockSnapshot,
            ReorderPointSnapshot = item.ReorderPointSnapshot,
            TargetStockSnapshot = item.TargetStockSnapshot,
            SuggestedQuantity = item.SuggestedQuantity,
            OrderedQuantity = item.OrderedQuantity == 0 ? item.Quantity : item.OrderedQuantity,
            PackageSizeSnapshot = item.PackageSizeSnapshot,
            MinimumOrderQuantitySnapshot = item.MinimumOrderQuantitySnapshot,
            UnitCostSnapshot = item.UnitCostSnapshot,
            EstimatedLineTotal = item.EstimatedLineTotal,
            RecommendationReason = string.IsNullOrWhiteSpace(item.RecommendationReason) ? item.Reasoning : item.RecommendationReason
        }).ToList()
    };
}
