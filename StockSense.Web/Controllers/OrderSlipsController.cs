using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Services;

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
    private readonly ILogger<OrderSlipsController> _logger;

    public OrderSlipsController(ApplicationDbContext context, IOrderSlipWorkflowService workflow, DocumentService docService, PdfDownloadCache pdfCache, ILogger<OrderSlipsController> logger)
    {
        _context = context;
        _workflow = workflow;
        _docService = docService;
        _pdfCache = pdfCache;
        _logger = logger;
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

    [HttpPost("{id:int}/place-order")]
    [Authorize(Roles = "Admin, Employee")]
    public async Task<IActionResult> PlaceOrder(int id, [FromBody] PlaceOrderCommand? command, CancellationToken cancellationToken)
    {
        command ??= new PlaceOrderCommand();
        command.OrderSlipId = id;
        try
        {
            var result = await _workflow.PlaceOrderAsync(command, cancellationToken);
            return !result.IsSuccess
                ? StatusCode(result.IsConcurrencyConflict ? 409 : 400, ApiResponse.Error(result.ErrorMessage ?? "The order could not be placed."))
                : Ok(new { message = "Order placed and email sent to supplier." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Placing order slip {OrderSlipId} failed.", id);
            return StatusCode(500, ApiResponse.Error("The order could not be placed. Please try again."));
        }
    }

    [HttpPost("{id:int}/close-short")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CloseShort(int id, [FromBody] CancelOrderSlipCommand? command, CancellationToken cancellationToken)
    {
        command ??= new CancelOrderSlipCommand();
        command.OrderSlipId = id;
        try
        {
            var result = await _workflow.CloseShortAsync(command, cancellationToken);
            return !result.IsSuccess
                ? StatusCode(result.IsConcurrencyConflict ? 409 : 400, ApiResponse.Error(result.ErrorMessage ?? "The order slip could not be closed short."))
                : Ok(new { message = "Order closed short." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Closing short order slip {OrderSlipId} failed.", id);
            return StatusCode(500, ApiResponse.Error("The order slip could not be closed short. Please try again."));
        }
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
