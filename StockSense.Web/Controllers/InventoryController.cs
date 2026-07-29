using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

namespace StockSense.Web.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = "Admin, Employee")]
public sealed class InventoryController : ControllerBase
{
    private const string LocationId = InventoryDefaults.LocationId;
    private readonly ApplicationDbContext _context;
    private readonly ISafetyStockCalculationService _calculationService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        ApplicationDbContext context,
        ISafetyStockCalculationService calculationService,
        ILogger<InventoryController> logger)
    {
        _context = context;
        _calculationService = calculationService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<IReadOnlyList<InventoryDashboardRowDto>>> GetDashboard(
        CancellationToken cancellationToken)
    {
        var incoming = await (
                from item in _context.OrderSlipItems.AsNoTracking()
                join slip in _context.OrderSlips.AsNoTracking() on item.OrderSlipId equals slip.Id
                where slip.LocationId == LocationId
                      && (slip.Status == OrderSlipStatuses.Approved
                          || slip.Status == OrderSlipStatuses.Ordered
                          || slip.Status == OrderSlipStatuses.PartiallyReceived)
                group item by item.ProductId into productItems
                select new
                {
                    ProductId = productItems.Key,
                    Quantity = productItems.Sum(item => item.OrderedQuantity - item.ReceivedQuantity)
                })
            .ToDictionaryAsync(row => row.ProductId, row => row.Quantity, cancellationToken);

        var rows = await (
                from product in _context.Products.AsNoTracking()
                join supplier in _context.Suppliers.AsNoTracking() on product.SupplierId equals supplier.Id into suppliers
                from supplier in suppliers.DefaultIfEmpty()
                join metric in _context.ProductInventoryMetrics.AsNoTracking().Where(x => x.LocationId == LocationId)
                    on product.Id equals metric.ProductId into metrics
                from metric in metrics.DefaultIfEmpty()
                join setting in _context.ProductInventorySettings.AsNoTracking().Where(x => x.LocationId == LocationId)
                    on product.Id equals setting.ProductId into settings
                from setting in settings.DefaultIfEmpty()
                orderby product.Name
                select new InventoryDashboardRowDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Category = product.Category,
                    Brand = product.Brand,
                    SupplierId = product.SupplierId,
                    SupplierName = supplier == null ? string.Empty : supplier.Name,
                    CurrentStock = product.CurrentStock,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    ProductRowVersion = product.RowVersion,
                    AverageDailyDemand = metric == null ? 0 : metric.AverageDailyDemand,
                    DemandStandardDeviation = metric == null ? 0 : metric.DemandStandardDeviation,
                    SafetyStock = metric == null ? 0 : metric.SafetyStock,
                    ReorderPoint = product.ReorderTarget,
                    TargetStock = metric == null ? 0 : metric.TargetStock,
                    CalculationStage = metric == null ? "Not calculated" : metric.CalculationStage,
                    ConfidenceLevel = metric == null ? "Low" : metric.ConfidenceLevel,
                    LastCalculatedAt = metric == null ? null : metric.LastCalculatedAt,
                    CalculationExplanation = metric == null ? "Run a calculation to create inventory metrics." : metric.CalculationReason ?? string.Empty,
                    IsAutomaticOrderEnabled = setting == null || setting.IsAutomaticOrderEnabled,
                    CalculationMode = setting == null ? InventoryCalculationModes.Auto : setting.CalculationMode
                })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            row.IncomingStock = incoming.GetValueOrDefault(row.ProductId);
            row.InventoryPosition = checked(row.CurrentStock + row.IncomingStock);
        }
        return Ok(rows);
    }

    [HttpGet("products/{productId:int}/settings")]
    public async Task<ActionResult<ProductInventorySettingDto>> GetSettings(int productId, CancellationToken cancellationToken)
    {
        var setting = await _context.ProductInventorySettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProductId == productId && x.LocationId == LocationId, cancellationToken);
        if (setting == null) return NotFound(new { error = "Inventory settings have not been initialized. Recalculate the product first." });
        return Ok(ToDto(setting));
    }

    [HttpPut("products/{productId:int}/settings")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SafetyStockCalculationResult>> UpdateSettings(
        int productId,
        ProductInventorySettingDto dto,
        CancellationToken cancellationToken)
    {
        if (productId != dto.ProductId) return BadRequest(new { error = "Product identifier does not match." });
        if (dto.RowVersion.Length == 0)
            return BadRequest(new { error = "A row version is required. Reload the latest settings and try again." });
        var validationError = Validate(dto);
        if (validationError != null) return BadRequest(new { error = validationError });

        var setting = await _context.ProductInventorySettings
            .SingleOrDefaultAsync(x => x.ProductId == productId && x.LocationId == LocationId, cancellationToken);
        if (setting == null) return NotFound(new { error = "Inventory settings were not found." });
        _context.Entry(setting).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;

        Apply(dto, setting);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "The record was changed by another user. Reload the latest data and try again." });
        }

        try
        {
            return Ok(await _calculationService.RecalculateProductAsync(productId, LocationId, cancellationToken));
        }
        catch (Exception exception)
        {
            // Settings are already committed, so return a successful save with a clear
            // retry path instead of inviting the administrator to overwrite them again.
            _logger.LogWarning(
                exception,
                "Inventory settings for product {ProductId} were saved, but recalculation did not complete.",
                productId);
            return Ok(new
            {
                message = "Inventory settings saved.",
                warning = "Safety-stock metrics could not be refreshed. Run recalculation again from inventory management."
            });
        }
    }

    [HttpPost("recalculate/{productId:int}")]
    public async Task<ActionResult<SafetyStockCalculationResult>> RecalculateProduct(int productId, CancellationToken cancellationToken) =>
        Ok(await _calculationService.RecalculateProductAsync(productId, LocationId, cancellationToken));

    [HttpPost("recalculate-selected")]
    public async Task<ActionResult<InventoryRecalculationSummaryDto>> RecalculateSelected(
        [FromBody] IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken)
    {
        var ids = productIds.Where(id => id > 0).Distinct().ToArray();
        var results = await _calculationService.RecalculateProductsAsync(ids, LocationId, cancellationToken);
        return Ok(new InventoryRecalculationSummaryDto(ids.Length, results.Count, results));
    }

    [HttpPost("recalculate-all")]
    public async Task<ActionResult<InventoryRecalculationSummaryDto>> RecalculateAll(CancellationToken cancellationToken)
    {
        var results = await _calculationService.RecalculateAllAsync(LocationId, cancellationToken);
        return Ok(new InventoryRecalculationSummaryDto(results.Count, results.Count, results));
    }

    private static ProductInventorySettingDto ToDto(ProductInventorySetting value) => new()
    {
        Id = value.Id, ProductId = value.ProductId, LocationId = value.LocationId,
        CalculationMode = value.CalculationMode, InitialEstimatedWeeklyDemand = value.InitialEstimatedWeeklyDemand,
        DefaultLeadTimeDays = value.DefaultLeadTimeDays, ReviewPeriodDays = value.ReviewPeriodDays,
        BufferDays = value.BufferDays, ServiceLevel = value.ServiceLevel,
        MinimumSafetyStock = value.MinimumSafetyStock, MaximumSafetyStock = value.MaximumSafetyStock,
        MinimumOrderQuantity = value.MinimumOrderQuantity, PackageSize = value.PackageSize,
        MaximumStockLevel = value.MaximumStockLevel, ManualSafetyStock = value.ManualSafetyStock,
        ManualReorderPoint = value.ManualReorderPoint, IsAutomaticOrderEnabled = value.IsAutomaticOrderEnabled,
        InventoryTrackingStartDate = value.InventoryTrackingStartDate, RowVersion = value.RowVersion
    };

    private static void Apply(ProductInventorySettingDto source, ProductInventorySetting target)
    {
        target.CalculationMode = source.CalculationMode;
        target.InitialEstimatedWeeklyDemand = source.InitialEstimatedWeeklyDemand;
        target.DefaultLeadTimeDays = source.DefaultLeadTimeDays;
        target.ReviewPeriodDays = source.ReviewPeriodDays;
        target.BufferDays = source.BufferDays;
        target.ServiceLevel = source.ServiceLevel;
        target.MinimumSafetyStock = source.MinimumSafetyStock;
        target.MaximumSafetyStock = source.MaximumSafetyStock;
        target.MinimumOrderQuantity = source.MinimumOrderQuantity;
        target.PackageSize = source.PackageSize;
        target.MaximumStockLevel = source.MaximumStockLevel;
        target.ManualSafetyStock = source.ManualSafetyStock;
        target.ManualReorderPoint = source.ManualReorderPoint;
        target.IsAutomaticOrderEnabled = source.IsAutomaticOrderEnabled;
        target.InventoryTrackingStartDate = source.InventoryTrackingStartDate;
    }

    private static string? Validate(ProductInventorySettingDto value)
    {
        if (value.CalculationMode is not (InventoryCalculationModes.Auto or InventoryCalculationModes.Manual)) return "Calculation mode must be Auto or Manual.";
        if (value.InitialEstimatedWeeklyDemand < 0) return "Estimated weekly demand cannot be negative.";
        if (value.DefaultLeadTimeDays < 1 || value.ReviewPeriodDays < 1) return "Lead time and review period must be at least one day.";
        if (value.BufferDays < 0 || value.MinimumSafetyStock < 0) return "Buffer and minimum safety stock cannot be negative.";
        if (value.ServiceLevel is < 0.50m or > 0.999m) return "Service level must be between 0.50 and 0.999.";
        if (value.MinimumOrderQuantity < 1 || value.PackageSize < 1) return "Minimum order quantity and package size must be at least one.";
        if (value.MaximumSafetyStock < value.MinimumSafetyStock) return "Maximum safety stock cannot be lower than minimum safety stock.";
        if (value.MaximumStockLevel <= 0) return "Maximum stock level must be empty or greater than zero.";
        if (value.CalculationMode == InventoryCalculationModes.Manual && (!value.ManualSafetyStock.HasValue || !value.ManualReorderPoint.HasValue)) return "Manual mode requires manual safety stock and reorder point values.";
        return null;
    }
}
