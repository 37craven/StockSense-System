using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

[Route("api/builds")]
[ApiController]
[Authorize]
public class BuildsController : ControllerBase
{
    private readonly BuildRequestRepository _buildRepo;
    private readonly IWorkOrderCheckoutService _checkoutService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BuildsController> _logger;
    private readonly MotorcycleRepository _motorcycleRepository;
    private readonly IAdminPinService? _adminPinService;

    public BuildsController(
        BuildRequestRepository buildRepo,
        IWorkOrderCheckoutService checkoutService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        MotorcycleRepository motorcycleRepository,
        ILogger<BuildsController> logger,
        IAdminPinService? adminPinService = null)
    {
        _buildRepo = buildRepo;
        _checkoutService = checkoutService;
        _userManager = userManager;
        _context = context;
        _adminPinService = adminPinService;
        _motorcycleRepository = motorcycleRepository;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBuild([FromBody] CreateBuildRequestDto dto)
    {
        if (dto == null) return BadRequest(ApiResponse.Error("Request is empty."));
        var customer = await _userManager.GetUserAsync(User);
        if (customer is null) return Unauthorized();

        List<ProductDto> submittedParts;
        try
        {
            submittedParts = JsonSerializer.Deserialize<List<ProductDto>>(dto.SelectedPartsJson) ?? [];
        }
        catch (JsonException)
        {
            return BadRequest(ApiResponse.Error("The selected parts are invalid. Refresh the build page and try again."));
        }

        var selectedProductIds = submittedParts
            .Where(part => part.Id > 0)
            .Select(part => part.Id)
            .ToList();
        if (selectedProductIds.Count == 0)
            return BadRequest(ApiResponse.Error("Select at least one active product before submitting the build."));

        var distinctProductIds = selectedProductIds.Distinct().ToList();
        var activeProducts = await _context.Products
            .AsNoTracking()
            .Where(product => distinctProductIds.Contains(product.Id) && product.IsActive)
            .ToListAsync();
        if (activeProducts.Count != distinctProductIds.Count)
            return BadRequest(ApiResponse.Error("One or more selected products are no longer available. Refresh the build page and choose active products."));

        var stockError = await StockAvailabilityValidator.ValidateAsync(
            _context, selectedProductIds, "build request", HttpContext.RequestAborted);
        if (stockError is not null)
            return Conflict(ApiResponse.Error(stockError));

        var productsById = activeProducts.ToDictionary(product => product.Id);
        var canonicalParts = selectedProductIds.Select(id => productsById[id]).Select(product => new ProductDto(
            product.Id,
            product.Name,
            product.Category,
            product.Brand,
            product.Price,
            product.CurrentStock,
            product.ReorderTarget,
            product.SupplierId ?? 0,
            string.Empty,
            product.ImageUrl ?? string.Empty,
            product.Barcode,
            product.UnitCost,
            product.RowVersion,
            product.IsActive)).ToList();

        // Preserve the UI's non-product build metadata, but never trust submitted product details or prices.
        canonicalParts.AddRange(submittedParts.Where(part => part.Id <= 0));

        Motorcycle? motorcycle = null;
        if (dto.MotorcycleId.HasValue)
        {
            motorcycle = await _motorcycleRepository.GetSelectableByIdAsync(dto.MotorcycleId.Value);
            if (motorcycle is null)
                return BadRequest(ApiResponse.Error("The selected motorcycle does not exist."));
        }

        var request = new BuildRequest
        {
            CustomerName = GetFullName(customer),
            CustomerEmail = customer.Email,
            CustomerUserId = customer.Id,
            BuildName = dto.BuildName,
            SelectedPartsJson = JsonSerializer.Serialize(canonicalParts),
            TotalPrice = selectedProductIds.Sum(id => productsById[id].Price),
            CreatedAt = DateTime.Now,
            Status = WorkOrderStatuses.Pending,
            MotorcycleId = motorcycle?.Id,
            Motorcycle = motorcycle
        };

        await _buildRepo.AddAsync(request);
        await _buildRepo.SaveChangesAsync();
        return Ok(MapToDto(request));
    }

    [HttpGet("all")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<ActionResult<List<BuildRequestDto>>> GetAllBuilds()
    {
        var builds = await _buildRepo.GetAllAsync();
        await EnrichCustomerIdentitiesAsync(builds);
        return Ok(builds.Select(MapToDto).ToList());
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateWorkOrderStatusDto request)
    {
        var build = await _context.BuildRequests.FindAsync(id);
        if (build == null) return NotFound(ApiResponse.NotFound("Build"));

        if (build.TransactionId.HasValue)
        {
            var txn = await _context.Transactions
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == build.TransactionId.Value);
            build.Transaction = txn;
        }

        if (string.Equals(request.Status, WorkOrderStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Error("Use the completion checkout endpoint to complete a build."));

        var canonicalStatus = new[] { WorkOrderStatuses.Pending, WorkOrderStatuses.Confirmed, WorkOrderStatuses.Cancelled }
            .SingleOrDefault(value => string.Equals(value, request.Status?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (canonicalStatus is null) return BadRequest(ApiResponse.Error("Unsupported build status."));
        var isAdmin = User.IsInRole("Admin");
        AdminPinVerificationResult? approval = null;
        if (!isAdmin && build.Status != WorkOrderStatuses.Pending)
        {
            if (_adminPinService is null) return StatusCode(403, ApiResponse.Error("Admin approval is required."));
            approval = !string.IsNullOrWhiteSpace(request.AdminUserId)
                ? await _adminPinService.VerifyByUserIdAsync(request.AdminUserId, request.AdminPin ?? "")
                : await _adminPinService.VerifyAsync(request.AdminEmail ?? "", request.AdminPin ?? "");
            if (!approval.Succeeded)
                return StatusCode(approval.LockedUntil.HasValue ? 429 : 403, ApiResponse.Error(approval.Error ?? "Admin approval failed."));
        }
        var transitionError = WorkOrderRules.ValidateStatusTransition(build.Status, canonicalStatus, isAdmin || approval?.Succeeded == true);
        if (transitionError is not null) return Conflict(ApiResponse.Error(transitionError));
        var reason = request.Reason?.Trim();
        if (WorkOrderRules.RequiresAdminReason(build.Status, canonicalStatus) && string.IsNullOrWhiteSpace(reason))
            return BadRequest(ApiResponse.Error("A reason is required for this admin action."));
        if (reason?.Length > 500)
            return BadRequest(ApiResponse.Error("The reason cannot exceed 500 characters."));

        try
        {
            if (canonicalStatus == WorkOrderStatuses.Pending && build.Transaction is not null)
            {
                await RestoreStockFromTransaction(build.Transaction);
                await _context.Transactions
                    .Where(t => t.Id == build.Transaction!.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsVoided, true));
                build.TransactionId = null;
                build.CompletedAt = null;
            }

            var previousStatus = build.Status;
            build.Status = canonicalStatus;
            AddAudit("Build", id, "StatusChanged", previousStatus, canonicalStatus, reason, approval);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Status updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reopening build {BuildId} failed.", id);
            return StatusCode(500, ApiResponse.Error("The build could not be reopened. Please try again."));
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

    [HttpPut("{id}/parts")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> UpdateParts(int id, [FromBody] UpdateBuildPartsDto dto)
    {
        var productIds = dto.ProductIds;
        var build = await _buildRepo.GetByIdAsync(id);
        if (build == null) return NotFound(ApiResponse.NotFound("Build"));

        if (build.Status is WorkOrderStatuses.Completed or WorkOrderStatuses.Cancelled)
            return BadRequest(ApiResponse.Error("Cannot modify parts on a completed or cancelled build."));
        AdminPinVerificationResult? approval = null;
        if (build.Status != WorkOrderStatuses.Pending && !User.IsInRole("Admin"))
        {
            if (_adminPinService is null) return StatusCode(403, ApiResponse.Error("Admin approval is required."));
            approval = !string.IsNullOrWhiteSpace(dto.AdminUserId)
                ? await _adminPinService.VerifyByUserIdAsync(dto.AdminUserId, dto.AdminPin ?? "")
                : await _adminPinService.VerifyAsync(dto.AdminEmail ?? "", dto.AdminPin ?? "");
            if (!approval.Succeeded)
                return StatusCode(approval.LockedUntil.HasValue ? 429 : 403, ApiResponse.Error(approval.Error ?? "Admin approval failed."));
        }
        var reason = dto.Reason?.Trim();
        if (build.Status != WorkOrderStatuses.Pending && string.IsNullOrWhiteSpace(reason))
            return BadRequest(ApiResponse.Error("Please provide a reason for this change."));

        var products = await _context.Products
            .Include(p => p.Supplier)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        if (products.Count != productIds.Count)
            return BadRequest(ApiResponse.Error("One or more products not found."));

        var partsJson = JsonSerializer.Serialize(products.Select(p => new
        {
            p.Id,
            p.Name,
            p.Category,
            p.Brand,
            p.Price,
            p.CurrentStock,
            p.ReorderTarget,
            SupplierId = p.SupplierId ?? 0,
            SupplierName = p.Supplier?.Name ?? "",
            ImageUrl = p.ImageUrl ?? ""
        }));

        build.SelectedPartsJson = partsJson;
        build.TotalPrice = products.Sum(p => p.Price);
        AddAudit("Build", id, "PartsChanged", null, string.Join(',', productIds), reason, approval);
        await _buildRepo.SaveChangesAsync();
        return Ok(new { message = "Parts updated.", totalPrice = build.TotalPrice });
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
            var receipt = await _checkoutService.CompleteBuildAsync(
                id,
                request,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                InventoryDefaults.LocationId,
                cancellationToken);
            return Ok(receipt);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse.NotFound("Build"));
        }
        catch (JsonException)
        {
            return BadRequest(ApiResponse.Error("The build's selected-parts data is invalid."));
        }
        catch (WorkOrderConflictException exception)
        {
            return Conflict(ApiResponse.Error(exception.Message));
        }
    }

    [HttpGet("mine")]
    public async Task<ActionResult<List<BuildRequestDto>>> GetCustomerBuilds()
    {
        var customer = await _userManager.GetUserAsync(User);
        if (customer is null) return Unauthorized();
        var builds = await _buildRepo.GetByCustomerIdentityAsync(
            customer.Id,
            customer.Email ?? string.Empty);
        return Ok(builds.Select(MapToDto).ToList());
    }

    private static BuildRequestDto MapToDto(BuildRequest build) => new()
    {
        Id = build.Id,
        CustomerName = build.CustomerName,
        CustomerEmail = build.CustomerEmail,
        BuildName = build.BuildName,
        SelectedPartsJson = build.SelectedPartsJson,
        TotalPrice = build.TotalPrice,
        CreatedAt = build.CreatedAt,
        Status = build.Status,
        CompletedAt = build.CompletedAt,
        TransactionId = build.TransactionId,
        InvoiceNumber = build.Transaction?.InvoiceNumber,
        MotorcycleId = build.MotorcycleId,
        Motorcycle = MapMotorcycle(build.Motorcycle)
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

    private async Task EnrichCustomerIdentitiesAsync(IEnumerable<BuildRequest> builds)
    {
        var users = await _userManager.Users.AsNoTracking().ToListAsync();
        foreach (var build in builds)
        {
            var user = users.FirstOrDefault(value => value.Id == build.CustomerUserId)
                ?? users.FirstOrDefault(value =>
                    string.Equals(value.Email, build.CustomerEmail, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value.Email, build.CustomerName, StringComparison.OrdinalIgnoreCase))
                ?? users.FirstOrDefault(value =>
                    string.Equals(GetFullName(value), build.CustomerName, StringComparison.OrdinalIgnoreCase));
            if (user is null) continue;
            build.CustomerName = GetFullName(user);
            build.CustomerEmail = user.Email;
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
}
