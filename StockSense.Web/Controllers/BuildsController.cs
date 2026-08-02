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

    public BuildsController(
        BuildRequestRepository buildRepo,
        IWorkOrderCheckoutService checkoutService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        MotorcycleRepository motorcycleRepository,
        ILogger<BuildsController> logger)
    {
        _buildRepo = buildRepo;
        _checkoutService = checkoutService;
        _userManager = userManager;
        _context = context;
        _motorcycleRepository = motorcycleRepository;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBuild([FromBody] CreateBuildRequestDto dto)
    {
        if (dto == null) return BadRequest(ApiResponse.Error("Request is empty."));
        var customer = await _userManager.GetUserAsync(User);
        if (customer is null) return Unauthorized();

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
            SelectedPartsJson = dto.SelectedPartsJson,
            TotalPrice = dto.TotalPrice,
            CreatedAt = DateTime.Now,
            Status = WorkOrderStatuses.Pending,
            MotorcycleId = motorcycle?.Id
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
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
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

        if (string.Equals(newStatus, WorkOrderStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Error("Use the completion checkout endpoint to complete a build."));

        var canonicalStatus = new[] { WorkOrderStatuses.Pending, WorkOrderStatuses.Confirmed, WorkOrderStatuses.Cancelled }
            .SingleOrDefault(value => string.Equals(value, newStatus?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (canonicalStatus is null) return BadRequest(ApiResponse.Error("Unsupported build status."));
        var transitionError = WorkOrderRules.ValidateStatusTransition(build.Status, canonicalStatus);
        if (transitionError is not null) return Conflict(ApiResponse.Error(transitionError));

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

            build.Status = canonicalStatus;
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
    public async Task<IActionResult> UpdateParts(int id, [FromBody] List<int> productIds)
    {
        var build = await _buildRepo.GetByIdAsync(id);
        if (build == null) return NotFound(ApiResponse.NotFound("Build"));

        if (build.Status is WorkOrderStatuses.Completed or WorkOrderStatuses.Cancelled)
            return BadRequest(ApiResponse.Error("Cannot modify parts on a completed or cancelled build."));

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
            customer.Email ?? string.Empty,
            GetFullName(customer));
        await EnrichCustomerIdentitiesAsync(builds);
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
}
