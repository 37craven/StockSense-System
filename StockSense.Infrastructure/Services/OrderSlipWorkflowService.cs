using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

namespace StockSense.Infrastructure.Services;

public sealed class OrderSlipWorkflowService : IOrderSlipWorkflowService
{
    private const string ConcurrencyMessage =
        "The order slip was changed by another user. Reload the latest data and try again.";
    private static readonly string[] OpenStatuses =
        [OrderSlipStatuses.Draft, OrderSlipStatuses.Approved, OrderSlipStatuses.Ordered, OrderSlipStatuses.PartiallyReceived];
    private static readonly string[] IncomingStatuses =
        [OrderSlipStatuses.Approved, OrderSlipStatuses.Ordered, OrderSlipStatuses.PartiallyReceived];

    private readonly ApplicationDbContext _context;
    private readonly ISafetyStockCalculationService _calculationService;
    private readonly ILogger<OrderSlipWorkflowService> _logger;

    public OrderSlipWorkflowService(
        ApplicationDbContext context,
        ISafetyStockCalculationService calculationService,
        ILogger<OrderSlipWorkflowService> logger)
    {
        _context = context;
        _calculationService = calculationService;
        _logger = logger;
    }

    public async Task<OperationResult<OrderSlipPreviewDto>> PreviewAsync(
        string locationId,
        CancellationToken cancellationToken = default)
    {
        locationId = NormalizeLocation(locationId);
        var products = await _context.Products.AsNoTracking().Include(product => product.Supplier)
            .OrderBy(product => product.Id).ToListAsync(cancellationToken);
        var productIds = products.Select(product => product.Id).ToArray();
        var settings = await _context.ProductInventorySettings.AsNoTracking()
            .Where(setting => productIds.Contains(setting.ProductId) && setting.LocationId == locationId)
            .ToDictionaryAsync(setting => setting.ProductId, cancellationToken);
        var metrics = await _context.ProductInventoryMetrics.AsNoTracking()
            .Where(metric => productIds.Contains(metric.ProductId) && metric.LocationId == locationId)
            .ToDictionaryAsync(metric => metric.ProductId, cancellationToken);
        var incoming = productIds.Length == 0
            ? new Dictionary<int, int>()
            : await _context.OrderSlipItems.AsNoTracking()
                .Where(item => productIds.Contains(item.ProductId)
                               && item.OrderSlip.LocationId == locationId
                               && IncomingStatuses.Contains(item.OrderSlip.Status))
                .GroupBy(item => item.ProductId)
                .ToDictionaryAsync(group => group.Key,
                    group => group.Sum(item =>
                        (item.OrderedQuantity > 0 ? item.OrderedQuantity : item.Quantity) - item.ReceivedQuantity), cancellationToken);
        var openProductIds = productIds.Length == 0
            ? []
            : await _context.OrderSlipItems.AsNoTracking()
                .Where(item => productIds.Contains(item.ProductId)
                               && item.OrderSlip.LocationId == locationId
                               && OpenStatuses.Contains(item.OrderSlip.Status))
                .Select(item => item.ProductId).Distinct().ToArrayAsync(cancellationToken);
        var openProducts = openProductIds.ToHashSet();

        var preview = new OrderSlipPreviewDto { LocationId = locationId, GeneratedAt = DateTime.Now };
        foreach (var product in products.OrderBy(product => product.SupplierId).ThenBy(product => product.Name))
        {
            if (!settings.TryGetValue(product.Id, out var setting))
            {
                preview.Warnings.Add(new(product.Id, product.Name, "MISSING_SETTINGS",
                    "Product cannot be evaluated because inventory settings are missing."));
                continue;
            }
            if (!setting.IsAutomaticOrderEnabled) continue;
            try { SafetyStockMath.ValidateSetting(setting); }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
            {
                preview.Warnings.Add(new(product.Id, product.Name, "INVALID_SETTINGS", exception.Message));
                continue;
            }
            if (!metrics.TryGetValue(product.Id, out var metric))
            {
                preview.Warnings.Add(new(product.Id, product.Name, "MISSING_METRIC",
                    "Product cannot be evaluated until safety stock is recalculated."));
                continue;
            }

            var currentIncoming = incoming.GetValueOrDefault(product.Id);
            var inventoryPosition = checked(product.CurrentStock + currentIncoming - product.ReservedStock);
            if (openProducts.Contains(product.Id))
            {
                preview.Warnings.Add(new(product.Id, product.Name, "OPEN_ORDER_EXISTS",
                    $"Skipped because the product already has an open order. Remaining incoming quantity: {currentIncoming}."));
                continue;
            }
            var suggested = OrderSlipMath.CalculateSuggestedQuantity(
                metric.TargetStock, product.ReorderTarget, inventoryPosition, setting.MinimumOrderQuantity,
                setting.PackageSize, setting.MaximumStockLevel);
            if (suggested == 0)
            {
                if (inventoryPosition <= product.ReorderTarget && metric.TargetStock > inventoryPosition)
                    preview.Warnings.Add(new(product.Id, product.Name, "NO_VALID_QUANTITY",
                        "No package-size quantity satisfies the configured minimum order and maximum stock level."));
                continue;
            }
            if (!product.SupplierId.HasValue)
            {
                preview.Warnings.Add(new(product.Id, product.Name, "MISSING_SUPPLIER",
                    "No supplier is assigned, so an order slip cannot be generated."));
                continue;
            }

            var group = preview.SupplierGroups.FirstOrDefault(value => value.SupplierId == product.SupplierId.Value);
            if (group is null)
            {
                group = new OrderSlipPreviewGroupDto
                {
                    SupplierId = product.SupplierId.Value,
                    SupplierName = product.Supplier?.Name ?? string.Empty
                };
                preview.SupplierGroups.Add(group);
            }

            group.Items.Add(new OrderSlipPreviewItemDto
            {
                ProductId = product.Id, ProductName = product.Name,
                Category = product.Category, Brand = product.Brand,
                CurrentStock = product.CurrentStock, IncomingStock = currentIncoming,
                ReservedStock = product.ReservedStock, InventoryPosition = inventoryPosition,
                AverageDailyDemand = metric.AverageDailyDemand,
                LeadTimeDays = metric.AverageLeadTimeDays,
                SafetyStock = metric.SafetyStock, ReorderPoint = product.ReorderTarget,
                TargetStock = metric.TargetStock, SuggestedQuantity = suggested,
                FinalQuantity = suggested, PackageSize = setting.PackageSize,
                MinimumOrderQuantity = setting.MinimumOrderQuantity,
                MaximumStockLevel = setting.MaximumStockLevel, UnitCost = product.UnitCost,
                EstimatedLineTotal = checked(product.UnitCost * suggested),
                RecommendationReason = metric.CalculationReason ?? "Inventory position is below target stock."
            });
        }

        return OperationResult<OrderSlipPreviewDto>.Success(preview);
    }

    public Task<OperationResult<CreateDraftOrderSlipsResult>> CreateDraftsAsync(
        CreateOrderSlipDraftsCommand command, CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(async ct =>
        {
            var locationId = NormalizeLocation(command.LocationId);
            if (command.SupplierGroups.Count == 0)
                return OperationResult<CreateDraftOrderSlipsResult>.Failure("EMPTY_ORDER", "Select at least one item to order.");
            if (command.SupplierGroups.Any(group => group.SupplierId <= 0 || group.Items.Count == 0)
                || command.SupplierGroups.Select(group => group.SupplierId).Distinct().Count() != command.SupplierGroups.Count
                || command.SupplierGroups.SelectMany(group => group.Items).GroupBy(item => item.ProductId).Any(group => group.Count() > 1))
                return OperationResult<CreateDraftOrderSlipsResult>.Failure("INVALID_ORDER", "Supplier groups and products must be unique and non-empty.");

            var productIds = command.SupplierGroups.SelectMany(group => group.Items).Select(item => item.ProductId).ToArray();
            var products = await _context.Products
                .Where(product => productIds.Contains(product.Id)).ToDictionaryAsync(product => product.Id, ct);
            var settings = await _context.ProductInventorySettings
                .Where(setting => productIds.Contains(setting.ProductId) && setting.LocationId == locationId)
                .ToDictionaryAsync(setting => setting.ProductId, ct);
            var metrics = await _context.ProductInventoryMetrics
                .Where(metric => productIds.Contains(metric.ProductId) && metric.LocationId == locationId)
                .ToDictionaryAsync(metric => metric.ProductId, ct);
            var existing = await _context.OrderSlipItems.AsNoTracking()
                .Where(item => productIds.Contains(item.ProductId) && item.OrderSlip.LocationId == locationId
                               && OpenStatuses.Contains(item.OrderSlip.Status))
                .Select(item => item.ProductId).Distinct().ToArrayAsync(ct);
            var existingIds = existing.ToHashSet();
            var incoming = await _context.OrderSlipItems.AsNoTracking()
                .Where(item => productIds.Contains(item.ProductId) && item.OrderSlip.LocationId == locationId
                               && IncomingStatuses.Contains(item.OrderSlip.Status))
                .GroupBy(item => item.ProductId)
                .ToDictionaryAsync(group => group.Key,
                    group => group.Sum(item =>
                        (item.OrderedQuantity > 0 ? item.OrderedQuantity : item.Quantity) - item.ReceivedQuantity), ct);

            var now = DateTime.Now;
            var slips = new List<OrderSlip>();
            var warnings = new List<OrderSlipGenerationWarningDto>();
            foreach (var group in command.SupplierGroups)
            {
                var slipNumber = $"ORD-{now:yyMMdd}-{now:HHss}-{InvoiceHelper.ShortCode()}";
                var slip = new OrderSlip
                {
                    SlipNumber = slipNumber, OrderSlipNumber = slipNumber,
                    DateGenerated = now, GeneratedAt = now, SupplierId = group.SupplierId,
                    LocationId = locationId, Status = OrderSlipStatuses.Draft,
                    ExpectedDeliveryDate = group.ExpectedDeliveryDate,
                    CreatedByUserId = command.CreatedByUserId, Remarks = command.Remarks
                };
                foreach (var requested in group.Items)
                {
                    if (!products.TryGetValue(requested.ProductId, out var product))
                    {
                        warnings.Add(new(requested.ProductId, $"Product {requested.ProductId}", "PRODUCT_NOT_FOUND",
                            "Skipped because the product no longer exists."));
                        continue;
                    }
                    if (existingIds.Contains(product.Id))
                    {
                        warnings.Add(new(product.Id, product.Name, "OPEN_ORDER_EXISTS",
                            $"Skipped because the product already has an open order. Remaining incoming quantity: {incoming.GetValueOrDefault(product.Id)}."));
                        continue;
                    }
                    if (product.SupplierId != group.SupplierId)
                    {
                        warnings.Add(new(product.Id, product.Name, "SUPPLIER_MISMATCH",
                            "Skipped because the assigned supplier changed after the preview was generated."));
                        continue;
                    }
                    if (!settings.TryGetValue(product.Id, out var setting))
                    {
                        warnings.Add(new(product.Id, product.Name, "MISSING_SETTINGS",
                            "Skipped because inventory settings are missing."));
                        continue;
                    }
                    if (!setting.IsAutomaticOrderEnabled)
                    {
                        warnings.Add(new(product.Id, product.Name, "AUTOMATIC_ORDER_DISABLED",
                            "Skipped because automatic ordering was disabled after the preview was generated."));
                        continue;
                    }
                    try { SafetyStockMath.ValidateSetting(setting); }
                    catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
                    {
                        warnings.Add(new(product.Id, product.Name, "INVALID_SETTINGS", exception.Message));
                        continue;
                    }
                    if (!metrics.TryGetValue(product.Id, out var metric))
                    {
                        warnings.Add(new(product.Id, product.Name, "MISSING_METRIC",
                            "Skipped because the safety-stock metric is missing."));
                        continue;
                    }
                    var currentIncoming = incoming.GetValueOrDefault(product.Id);
                    var inventoryPosition = checked(product.CurrentStock + currentIncoming - product.ReservedStock);
                    var validation = OrderSlipMath.ValidateOrderedQuantity(requested.OrderedQuantity,
                        setting.MinimumOrderQuantity, setting.PackageSize, inventoryPosition, setting.MaximumStockLevel);
                    if (validation is not null)
                    {
                        warnings.Add(new(product.Id, product.Name, "INVALID_QUANTITY", validation));
                        continue;
                    }
                    if (inventoryPosition > product.ReorderTarget)
                    {
                        warnings.Add(new(product.Id, product.Name, "NO_LONGER_AT_REORDER_POINT",
                            "Skipped because current inventory position is now above the reorder point."));
                        continue;
                    }
                    var suggested = OrderSlipMath.CalculateSuggestedQuantity(metric.TargetStock, product.ReorderTarget, inventoryPosition,
                        setting.MinimumOrderQuantity, setting.PackageSize, setting.MaximumStockLevel);
                    if (suggested == 0)
                    {
                        warnings.Add(new(product.Id, product.Name, "NO_VALID_QUANTITY",
                            "Skipped because no valid order quantity remains under the current inventory rules."));
                        continue;
                    }
                    var lineTotal = checked(product.UnitCost * requested.OrderedQuantity);
                    slip.Items.Add(new OrderSlipItem
                    {
                        ProductId = product.Id, ProductName = product.Name, Brand = product.Brand,
                        Category = product.Category, CurrentStock = product.CurrentStock,
                        ReorderTarget = product.ReorderTarget, Quantity = requested.OrderedQuantity,
                        OrderedQuantity = requested.OrderedQuantity, ReceivedQuantity = 0,
                        CurrentStockSnapshot = product.CurrentStock, IncomingStockSnapshot = currentIncoming,
                        ReservedStockSnapshot = product.ReservedStock,
                        InventoryPositionSnapshot = inventoryPosition,
                        AverageDailyDemandSnapshot = metric.AverageDailyDemand,
                        LeadTimeDaysSnapshot = metric.AverageLeadTimeDays,
                        SafetyStockSnapshot = metric.SafetyStock, ReorderPointSnapshot = product.ReorderTarget,
                        TargetStockSnapshot = metric.TargetStock, SuggestedQuantity = suggested,
                        PackageSizeSnapshot = setting.PackageSize,
                        MinimumOrderQuantitySnapshot = setting.MinimumOrderQuantity,
                        UnitCostSnapshot = product.UnitCost, EstimatedLineTotal = lineTotal,
                        RecommendationReason = metric.CalculationReason ?? "Inventory position is below target stock."
                    });
                    slip.TotalEstimatedCost = checked(slip.TotalEstimatedCost + lineTotal);
                }
                if (slip.Items.Count == 0) continue;
                slips.Add(slip);
                await _context.OrderSlips.AddAsync(slip, ct);
            }
            await _context.SaveChangesAsync(ct);
            foreach (var slip in slips)
                await _context.Entry(slip).Reference(value => value.Supplier).LoadAsync(ct);
            return OperationResult<CreateDraftOrderSlipsResult>.Success(
                new(slips.Select(ToDto).ToArray(), warnings));
        }, cancellationToken);

    public async Task<OperationResult<ManualOrderSlipCatalogDto>> GetManualCatalogAsync(
        string locationId, CancellationToken cancellationToken = default)
    {
        locationId = NormalizeLocation(locationId);
        var suppliers = await _context.Suppliers.AsNoTracking()
            .OrderBy(supplier => supplier.Name)
            .Select(supplier => new ManualOrderSlipSupplierDto { Id = supplier.Id, Name = supplier.Name })
            .ToListAsync(cancellationToken);
        var products = await _context.Products.AsNoTracking()
            .Where(product => product.SupplierId.HasValue)
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);
        var productIds = products.Select(product => product.Id).ToArray();
        var settings = await _context.ProductInventorySettings.AsNoTracking()
            .Where(setting => productIds.Contains(setting.ProductId) && setting.LocationId == locationId)
            .ToDictionaryAsync(setting => setting.ProductId, cancellationToken);

        return OperationResult<ManualOrderSlipCatalogDto>.Success(new ManualOrderSlipCatalogDto
        {
            Suppliers = suppliers,
            Products = products.Select(product =>
            {
                var hasSetting = settings.TryGetValue(product.Id, out var setting);
                return new ManualOrderSlipProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Brand = product.Brand,
                    Category = product.Category,
                    SupplierId = product.SupplierId!.Value,
                    UnitCost = product.UnitCost,
                    CurrentStock = product.CurrentStock,
                    MinimumOrderQuantity = hasSetting ? setting!.MinimumOrderQuantity : 1,
                    PackageSize = hasSetting ? setting!.PackageSize : 1,
                    MaximumStockLevel = hasSetting ? setting!.MaximumStockLevel : null,
                    HasInventorySettings = hasSetting
                };
            }).ToList()
        });
    }

    public Task<OperationResult<OrderSlipDto>> CreateManualDraftAsync(
        CreateManualOrderSlipDraftCommand command, CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(async ct =>
        {
            var locationId = NormalizeLocation(command.LocationId);
            var reason = command.Reason?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(reason))
                return OperationResult<OrderSlipDto>.Failure("REASON_REQUIRED", "Enter a reason for this manual order.");
            if (reason.Length > 500)
                return OperationResult<OrderSlipDto>.Failure("REASON_TOO_LONG", "The manual order reason cannot exceed 500 characters.");
            if (command.ExpectedDeliveryDate.HasValue && command.ExpectedDeliveryDate.Value.Date < DateTime.Today)
                return OperationResult<OrderSlipDto>.Failure("INVALID_EXPECTED_DATE", "Expected delivery date cannot be in the past.");
            if (command.SupplierId <= 0 || command.Items.Count == 0
                || command.Items.Any(item => item.ProductId <= 0 || item.OrderedQuantity <= 0)
                || command.Items.Select(item => item.ProductId).Distinct().Count() != command.Items.Count)
                return OperationResult<OrderSlipDto>.Failure("INVALID_ORDER", "Select a supplier and add unique products with positive quantities.");

            var supplier = await _context.Suppliers.SingleOrDefaultAsync(value => value.Id == command.SupplierId, ct);
            if (supplier is null)
                return OperationResult<OrderSlipDto>.Failure("SUPPLIER_NOT_FOUND", "The selected supplier no longer exists.");

            var productIds = command.Items.Select(item => item.ProductId).ToArray();
            var products = await _context.Products.Where(product => productIds.Contains(product.Id))
                .ToDictionaryAsync(product => product.Id, ct);
            if (products.Count != productIds.Length)
                return OperationResult<OrderSlipDto>.Failure("PRODUCT_NOT_FOUND", "One or more selected products no longer exist.");
            if (products.Values.Any(product => product.SupplierId != command.SupplierId))
                return OperationResult<OrderSlipDto>.Failure("SUPPLIER_MISMATCH", "Every product must be assigned to the selected supplier.");

            var existingProductIds = await _context.OrderSlipItems.AsNoTracking()
                .Where(item => productIds.Contains(item.ProductId)
                               && item.OrderSlip.LocationId == locationId
                               && OpenStatuses.Contains(item.OrderSlip.Status))
                .Select(item => item.ProductId).Distinct().ToArrayAsync(ct);
            if (existingProductIds.Length > 0)
                return OperationResult<OrderSlipDto>.Failure("OPEN_ORDER_EXISTS", "A selected product already has an open order slip.");

            var settings = await _context.ProductInventorySettings.AsNoTracking()
                .Where(setting => productIds.Contains(setting.ProductId) && setting.LocationId == locationId)
                .ToDictionaryAsync(setting => setting.ProductId, ct);
            var metrics = await _context.ProductInventoryMetrics.AsNoTracking()
                .Where(metric => productIds.Contains(metric.ProductId) && metric.LocationId == locationId)
                .ToDictionaryAsync(metric => metric.ProductId, ct);

            foreach (var requested in command.Items)
            {
                if (!settings.TryGetValue(requested.ProductId, out var setting)) continue;
                if (setting.MinimumOrderQuantity <= 0 || setting.PackageSize <= 0)
                    return OperationResult<OrderSlipDto>.Failure("INVALID_SETTINGS", "A selected product has invalid order quantity settings.");
                var validation = OrderSlipMath.ValidateOrderedQuantity(
                    requested.OrderedQuantity, setting.MinimumOrderQuantity, setting.PackageSize,
                    products[requested.ProductId].CurrentStock, setting.MaximumStockLevel);
                if (validation is not null)
                    return OperationResult<OrderSlipDto>.Failure("INVALID_QUANTITY", $"{products[requested.ProductId].Name}: {validation}");
            }

            var now = DateTime.Now;
            var slipNumber = $"ORD-{now:yyMMdd}-{now:HHss}-{InvoiceHelper.ShortCode()}";
            var slip = new OrderSlip
            {
                SlipNumber = slipNumber,
                OrderSlipNumber = slipNumber,
                DateGenerated = now,
                GeneratedAt = now,
                SupplierId = supplier.Id,
                Supplier = supplier,
                LocationId = locationId,
                Status = OrderSlipStatuses.Draft,
                ExpectedDeliveryDate = command.ExpectedDeliveryDate?.Date,
                CreatedByUserId = command.CreatedByUserId,
                Remarks = reason
            };
            foreach (var requested in command.Items)
            {
                var product = products[requested.ProductId];
                settings.TryGetValue(product.Id, out var setting);
                metrics.TryGetValue(product.Id, out var metric);
                var lineTotal = checked(product.UnitCost * requested.OrderedQuantity);
                slip.Items.Add(new OrderSlipItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Brand = product.Brand,
                    Category = product.Category,
                    CurrentStock = product.CurrentStock,
                    ReorderTarget = product.ReorderTarget,
                    Quantity = requested.OrderedQuantity,
                    OrderedQuantity = requested.OrderedQuantity,
                    CurrentStockSnapshot = product.CurrentStock,
                    InventoryPositionSnapshot = product.CurrentStock,
                    ReservedStockSnapshot = product.ReservedStock,
                    AverageDailyDemandSnapshot = metric?.AverageDailyDemand ?? 0,
                    LeadTimeDaysSnapshot = metric?.AverageLeadTimeDays ?? 0,
                    SafetyStockSnapshot = metric?.SafetyStock ?? 0,
                    ReorderPointSnapshot = product.ReorderTarget,
                    TargetStockSnapshot = metric?.TargetStock ?? product.ReorderTarget,
                    SuggestedQuantity = requested.OrderedQuantity,
                    PackageSizeSnapshot = setting?.PackageSize ?? 1,
                    MinimumOrderQuantitySnapshot = setting?.MinimumOrderQuantity ?? 1,
                    UnitCostSnapshot = product.UnitCost,
                    EstimatedLineTotal = lineTotal,
                    RecommendationReason = $"Manual order: {reason}"
                });
                slip.TotalEstimatedCost = checked(slip.TotalEstimatedCost + lineTotal);
            }

            await _context.OrderSlips.AddAsync(slip, ct);
            await _context.SaveChangesAsync(ct);
            return OperationResult<OrderSlipDto>.Success(ToDto(slip));
        }, cancellationToken);

    public Task<OperationResult<OrderSlipDto>> ApproveAsync(OrderSlipTransitionCommand command, CancellationToken cancellationToken = default) =>
        TransitionAsync(command, OrderSlipStatuses.Draft, OrderSlipStatuses.Approved, cancellationToken);

    public Task<OperationResult<OrderSlipDto>> MarkOrderedAsync(OrderSlipTransitionCommand command, CancellationToken cancellationToken = default) =>
        TransitionAsync(command, OrderSlipStatuses.Approved, OrderSlipStatuses.Ordered, cancellationToken);

    public async Task<OperationResult<OrderSlipDto>> CloseShortAsync(
        CloseOrderSlipShortCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteWriteAsync(async ct =>
        {
            var slip = await LoadSlipAsync(command.OrderSlipId, ct);
            if (slip is null) return OperationResult<OrderSlipDto>.Failure("NOT_FOUND", "Order slip was not found.");

            var hasReceived = slip.Items.Any(item => item.ReceivedQuantity > 0);
            var hasRemaining = slip.Items.Any(item => OrderSlipMath.CalculateRemainingQuantity(
                item.OrderedQuantity, item.Quantity, item.ReceivedQuantity) > 0);
            var validationError = OrderSlipMath.ValidateCloseShort(
                slip.Status, hasReceived, hasRemaining, command.Reason);
            if (validationError is not null)
            {
                var code = string.IsNullOrWhiteSpace(command.Reason)
                    ? "CLOSE_REASON_REQUIRED"
                    : "INVALID_STATUS";
                return OperationResult<OrderSlipDto>.Failure(code, validationError);
            }

            ApplyRowVersion(slip, command.RowVersion);
            slip.Status = OrderSlipStatuses.ClosedShort;
            slip.CompletedAt = DateTime.Now;
            slip.IsReceived = false;
            var remainingSummary = string.Join(", ", slip.Items
                .Select(item => new
                {
                    item.ProductName,
                    Remaining = OrderSlipMath.CalculateRemainingQuantity(
                        item.OrderedQuantity, item.Quantity, item.ReceivedQuantity)
                })
                .Where(item => item.Remaining > 0)
                .Select(item => $"{item.ProductName}: {item.Remaining}"));
            var actor = string.IsNullOrWhiteSpace(command.ActingUserId) ? "an administrator" : command.ActingUserId;
            var closeRemark = AppendRemark(slip.Remarks,
                $"Closed with remaining items by {actor}. Reason: {command.Reason.Trim()}. Outstanding: {remainingSummary}");
            slip.Remarks = closeRemark.Length <= 500 ? closeRemark : closeRemark[..500];
            _context.WorkOrderAudits.Add(new WorkOrderAudit
            {
                WorkOrderType = "OrderSlip",
                WorkOrderId = slip.Id,
                Action = "ClosedShort",
                PreviousValue = OrderSlipStatuses.PartiallyReceived,
                NewValue = OrderSlipStatuses.ClosedShort,
                ActorUserId = command.ActingUserId ?? string.Empty,
                ActorRole = command.ActorRole is "Admin" ? "Admin" : "Employee",
                ApproverUserId = command.ApproverUserId,
                ApproverEmail = command.ApproverEmail,
                Reason = command.Reason.Trim(),
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync(ct);
            return OperationResult<OrderSlipDto>.Success(ToDto(slip));
        }, cancellationToken);

        if (result.IsSuccess && result.Value is not null)
            await TryRecalculateProductsAsync(
                result.Value.Items.Select(item => item.ProductId),
                result.Value.LocationId,
                "closing the remaining order");
        return result;
    }

    public async Task<OperationResult<OrderSlipDto>> CancelAsync(
        CancelOrderSlipCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteWriteAsync(async ct =>
        {
            var slip = await LoadSlipAsync(command.OrderSlipId, ct);
            if (slip is null) return OperationResult<OrderSlipDto>.Failure("NOT_FOUND", "Order slip was not found.");
            var cancellationError = OrderSlipMath.ValidateCancellation(
                slip.Status, slip.Items.Any(item => item.ReceivedQuantity > 0), command.Reason);
            if (cancellationError is not null)
                return OperationResult<OrderSlipDto>.Failure(
                    string.IsNullOrWhiteSpace(command.Reason) ? "CANCELLATION_REASON_REQUIRED" : "INVALID_STATUS",
                    cancellationError);
            ApplyRowVersion(slip, command.RowVersion);
            slip.Status = OrderSlipStatuses.Cancelled;
            slip.Remarks = AppendRemark(slip.Remarks, command.Reason);
            await _context.SaveChangesAsync(ct);
            return OperationResult<OrderSlipDto>.Success(ToDto(slip));
        }, cancellationToken);

        if (result.IsSuccess && result.Value is not null)
            await TryRecalculateProductsAsync(
                result.Value.Items.Select(item => item.ProductId),
                result.Value.LocationId,
                "order-slip cancellation");
        return result;
    }

    public async Task<OperationResult<OrderSlipReceiptResult>> ReceiveAsync(
        ReceiveOrderSlipCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteWriteAsync(async ct =>
        {
            var slip = await LoadSlipAsync(command.OrderSlipId, ct);
            if (slip is null) return OperationResult<OrderSlipReceiptResult>.Failure("NOT_FOUND", "Order slip was not found.");
            if (slip.Status is not (OrderSlipStatuses.Ordered or OrderSlipStatuses.PartiallyReceived))
                return OperationResult<OrderSlipReceiptResult>.Failure("INVALID_STATUS", "Only ordered slips can receive stock.");
            if (!string.Equals(slip.LocationId, NormalizeLocation(command.LocationId), StringComparison.OrdinalIgnoreCase))
                return OperationResult<OrderSlipReceiptResult>.Failure("LOCATION_MISMATCH", "Receipt location must match the order slip location.");
            if (command.Items.Count == 0 || command.Items.Any(item => item.QuantityReceived <= 0)
                || command.Items.GroupBy(item => item.OrderSlipItemId).Any(group => group.Count() > 1))
                return OperationResult<OrderSlipReceiptResult>.Failure("INVALID_RECEIPT", "Receipt items must be unique and quantities must be greater than zero.");
            ApplyRowVersion(slip, command.RowVersion);

            var itemById = slip.Items.ToDictionary(item => item.Id);
            var productIds = command.Items.Where(item => itemById.ContainsKey(item.OrderSlipItemId))
                .Select(item => itemById[item.OrderSlipItemId].ProductId).ToArray();
            if (productIds.Length != command.Items.Count)
                return OperationResult<OrderSlipReceiptResult>.Failure("INVALID_RECEIPT_ITEM", "One or more receipt items do not belong to the order slip.");
            var products = await _context.Products.Where(product => productIds.Contains(product.Id))
                .ToDictionaryAsync(product => product.Id, ct);

            var receivedAt = command.ReceivedAt == default ? DateTime.Now : command.ReceivedAt;
            var receiptDateError = OrderSlipMath.ValidateReceiptDate(receivedAt, slip.OrderedAt, DateTime.Today);
            if (receiptDateError is not null)
                return OperationResult<OrderSlipReceiptResult>.Failure("INVALID_RECEIPT_DATE", receiptDateError);
            var number = $"PRC-{receivedAt:yyMMdd}-{receivedAt:HHss}-{InvoiceHelper.ShortCode()}";
            var receipt = new Transaction
            {
                InvoiceNumber = number, TransactionDate = receivedAt,
                TransactionType = TransactionTypes.PurchaseReceipt, PaymentMethod = "N/A",
                ReferenceNumber = string.IsNullOrWhiteSpace(command.ReferenceNumber)
                    ? (string.IsNullOrWhiteSpace(slip.OrderSlipNumber) ? slip.SlipNumber : slip.OrderSlipNumber)
                    : command.ReferenceNumber.Trim(),
                UserId = command.ReceivedByUserId,
                LocationId = slip.LocationId, Remarks = command.Remarks, OrderSlipId = slip.Id
            };
            foreach (var request in command.Items)
            {
                var item = itemById[request.OrderSlipItemId];
                var orderedQuantity = OrderSlipMath.ResolveOrderedQuantity(item.OrderedQuantity, item.Quantity);
                var remaining = orderedQuantity - item.ReceivedQuantity;
                if (request.QuantityReceived > remaining)
                    return OperationResult<OrderSlipReceiptResult>.Failure("OVER_RECEIPT", $"Receipt quantity for {item.ProductName} exceeds the remaining {remaining}.");
                if (!products.TryGetValue(item.ProductId, out var product))
                    return OperationResult<OrderSlipReceiptResult>.Failure("PRODUCT_NOT_FOUND", $"Product {item.ProductName} no longer exists.");
                var before = product.CurrentStock;
                product.AddStock(request.QuantityReceived);
                item.ReceivedQuantity = checked(item.ReceivedQuantity + request.QuantityReceived);
                var lineTotal = checked(item.UnitCostSnapshot * request.QuantityReceived);
                receipt.Items.Add(new TransactionItem
                {
                    ProductId = product.Id, ProductName = item.ProductName,
                    UnitPrice = item.UnitCostSnapshot, UnitCost = item.UnitCostSnapshot,
                    Quantity = request.QuantityReceived, StockBefore = before, StockAfter = product.CurrentStock,
                    LineTotal = lineTotal, OrderSlipItemId = item.Id
                });
                receipt.TotalAmount = checked(receipt.TotalAmount + lineTotal);
            }
            slip.Status = slip.Items.All(item =>
                    item.ReceivedQuantity == OrderSlipMath.ResolveOrderedQuantity(item.OrderedQuantity, item.Quantity))
                ? OrderSlipStatuses.Completed : OrderSlipStatuses.PartiallyReceived;
            // A partial receipt may leave the textual status unchanged. Force an UPDATE so the
            // order-slip rowversion still serializes concurrent receipts for the same slip.
            _context.Entry(slip).Property(value => value.Status).IsModified = true;
            if (slip.Status == OrderSlipStatuses.Completed)
            {
                slip.CompletedAt = receivedAt;
                slip.IsReceived = true;
            }
            await _context.Transactions.AddAsync(receipt, ct);
            await _context.SaveChangesAsync(ct);
            return OperationResult<OrderSlipReceiptResult>.Success(
                new(slip.Id, receipt.Id, receipt.InvoiceNumber, slip.Status, productIds.Distinct().ToArray()));
        }, cancellationToken);

        if (result.IsSuccess && result.Value is not null)
            await TryRecalculateAfterReceiptAsync(result.Value);
        return result;
    }

    private Task<OperationResult<OrderSlipDto>> TransitionAsync(
        OrderSlipTransitionCommand command, string expectedStatus, string newStatus, CancellationToken cancellationToken) =>
        ExecuteWriteAsync(async ct =>
        {
            if (!string.IsNullOrWhiteSpace(command.TargetStatus)
                && !string.Equals(command.TargetStatus, newStatus, StringComparison.OrdinalIgnoreCase))
                return OperationResult<OrderSlipDto>.Failure("INVALID_TARGET_STATUS", $"Target status must be {newStatus}.");
            var slip = await LoadSlipAsync(command.OrderSlipId, ct);
            if (slip is null) return OperationResult<OrderSlipDto>.Failure("NOT_FOUND", "Order slip was not found.");
            var transitionError = OrderSlipMath.ValidateTransition(slip.Status, newStatus);
            if (slip.Status != expectedStatus || transitionError is not null)
                return OperationResult<OrderSlipDto>.Failure("INVALID_STATUS",
                    transitionError ?? $"Only {expectedStatus} order slips can transition to {newStatus}.");
            ApplyRowVersion(slip, command.RowVersion);
            slip.Status = newStatus;
            slip.Remarks = string.IsNullOrWhiteSpace(command.Remarks) ? slip.Remarks : command.Remarks.Trim();
            if (command.ExpectedDeliveryDate.HasValue) slip.ExpectedDeliveryDate = command.ExpectedDeliveryDate;
            if (newStatus == OrderSlipStatuses.Approved)
            {
                slip.ApprovedAt = DateTime.Now;
                slip.ApprovedByUserId = command.ActingUserId;
            }
            else slip.OrderedAt = DateTime.Now;
            await _context.SaveChangesAsync(ct);
            return OperationResult<OrderSlipDto>.Success(ToDto(slip));
        }, cancellationToken);

    private async Task TryRecalculateAfterReceiptAsync(OrderSlipReceiptResult receipt)
    {
        try
        {
            var slip = await _context.OrderSlips.AsNoTracking()
                .Where(value => value.Id == receipt.OrderSlipId)
                .Select(value => new { value.SupplierId, value.LocationId })
                .SingleAsync(CancellationToken.None);
            int[] supplierProductIds = [];
            if (receipt.OrderSlipStatus == OrderSlipStatuses.Completed)
            {
                supplierProductIds = await _context.Products.AsNoTracking()
                    .Where(product => product.SupplierId == slip.SupplierId)
                    .Select(product => product.Id)
                    .ToArrayAsync(CancellationToken.None);
            }
            var productIds = OrderSlipMath.ResolveReceiptRecalculationProductIds(
                receipt.AffectedProductIds,
                supplierProductIds,
                receipt.OrderSlipStatus == OrderSlipStatuses.Completed);

            await _calculationService.RecalculateProductsAsync(
                productIds, slip.LocationId, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Order slip {OrderSlipId} was received successfully, but post-commit inventory recalculation failed.",
                receipt.OrderSlipId);
        }
    }

    private async Task TryRecalculateProductsAsync(
        IEnumerable<int> productIds,
        string locationId,
        string trigger)
    {
        try
        {
            var ids = productIds.Distinct().ToArray();
            if (ids.Length == 0) return;
            await _calculationService.RecalculateProductsAsync(ids, locationId, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "The {Trigger} committed successfully, but post-commit inventory recalculation failed.",
                trigger);
        }
    }

    private async Task<OperationResult<T>> ExecuteWriteAsync<T>(
        Func<CancellationToken, Task<OperationResult<T>>> action, CancellationToken cancellationToken)
    {
        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                var result = await action(cancellationToken);
                if (result.IsSuccess) await transaction.CommitAsync(cancellationToken);
                else await transaction.RollbackAsync(cancellationToken);
                return result;
            });
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(exception, "Order slip workflow encountered a concurrency conflict.");
            return OperationResult<T>.Failure("CONCURRENCY_CONFLICT", ConcurrencyMessage, true);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Order slip workflow could not persist a database update.");
            return OperationResult<T>.Failure("DATABASE_CONFLICT", "The order slip could not be saved because related inventory changed. Reload and try again.");
        }
    }

    private Task<OrderSlip?> LoadSlipAsync(int id, CancellationToken ct) => _context.OrderSlips
        .Include(slip => slip.Supplier).Include(slip => slip.Items).FirstOrDefaultAsync(slip => slip.Id == id, ct);

    private void ApplyRowVersion(OrderSlip slip, byte[] rowVersion)
    {
        if (rowVersion.Length == 0 || !rowVersion.SequenceEqual(slip.RowVersion))
            throw new DbUpdateConcurrencyException(ConcurrencyMessage);
        _context.Entry(slip).Property(value => value.RowVersion).OriginalValue = rowVersion;
    }

    private static string NormalizeLocation(string locationId)
    {
        var value = string.IsNullOrWhiteSpace(locationId) ? InventoryDefaults.LocationId : locationId.Trim();
        if (value.Length > 50) throw new InvalidOperationException("Location identifier cannot exceed 50 characters.");
        return value;
    }

    private static string AppendRemark(string? existing, string addition) =>
        string.IsNullOrWhiteSpace(existing) ? addition.Trim() : $"{existing.Trim()} | {addition.Trim()}";

    private static OrderSlipDto ToDto(OrderSlip slip) => new()
    {
        Id = slip.Id, SlipNumber = slip.SlipNumber, OrderSlipNumber = slip.OrderSlipNumber,
        DateGenerated = slip.DateGenerated, GeneratedAt = slip.GeneratedAt,
        SupplierId = slip.SupplierId, SupplierName = slip.Supplier?.Name ?? string.Empty,
        SupplierEmail = slip.Supplier?.Email ?? string.Empty, IsReceived = slip.IsReceived,
        LocationId = slip.LocationId, Status = slip.Status, ApprovedAt = slip.ApprovedAt,
        OrderedAt = slip.OrderedAt, ExpectedDeliveryDate = slip.ExpectedDeliveryDate,
        CompletedAt = slip.CompletedAt, CreatedByUserId = slip.CreatedByUserId,
        ApprovedByUserId = slip.ApprovedByUserId, TotalEstimatedCost = slip.TotalEstimatedCost,
        Remarks = slip.Remarks, RowVersion = slip.RowVersion,
        Items = slip.Items.Select(item => new OrderSlipItemDto
        {
            Id = item.Id, ProductId = item.ProductId, ProductName = item.ProductName,
            Brand = item.Brand, Category = item.Category ?? string.Empty, CurrentStock = item.CurrentStock,
            ReorderTarget = item.ReorderTarget, Quantity = item.Quantity, ReceivedQuantity = item.ReceivedQuantity,
            CurrentStockSnapshot = item.CurrentStockSnapshot, IncomingStockSnapshot = item.IncomingStockSnapshot,
            ReservedStockSnapshot = item.ReservedStockSnapshot,
            InventoryPositionSnapshot = item.InventoryPositionSnapshot,
            AverageDailyDemandSnapshot = item.AverageDailyDemandSnapshot,
            LeadTimeDaysSnapshot = item.LeadTimeDaysSnapshot, SafetyStockSnapshot = item.SafetyStockSnapshot,
            ReorderPointSnapshot = item.ReorderPointSnapshot, TargetStockSnapshot = item.TargetStockSnapshot,
            SuggestedQuantity = item.SuggestedQuantity, OrderedQuantity = item.OrderedQuantity,
            PackageSizeSnapshot = item.PackageSizeSnapshot,
            MinimumOrderQuantitySnapshot = item.MinimumOrderQuantitySnapshot,
            UnitCostSnapshot = item.UnitCostSnapshot, EstimatedLineTotal = item.EstimatedLineTotal,
            RecommendationReason = item.RecommendationReason
        }).ToList()
    };
}
