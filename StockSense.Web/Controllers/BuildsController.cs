using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
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
    private readonly IBuildRequestSubmissionService _submissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public BuildsController(
        BuildRequestRepository buildRepo,
        IWorkOrderCheckoutService checkoutService,
        IBuildRequestSubmissionService submissionService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _buildRepo = buildRepo;
        _checkoutService = checkoutService;
        _submissionService = submissionService;
        _userManager = userManager;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBuild([FromBody] CreateBuildRequestDto dto)
    {
        if (dto == null) return BadRequest(ApiResponse.Error("Request is empty."));
        var customer = await _userManager.GetUserAsync(User);
        if (customer is null) return Unauthorized();

        var request = new BuildRequest
        {
            CustomerName = GetFullName(customer),
            CustomerEmail = customer.Email,
            CustomerUserId = customer.Id,
            BuildName = dto.BuildName,
            SelectedPartsJson = dto.SelectedPartsJson,
            TotalPrice = dto.TotalPrice,
            CreatedAt = DateTime.Now,
            Status = WorkOrderStatuses.Pending
        };

        await _buildRepo.AddAsync(request);
        await _buildRepo.SaveChangesAsync();
        return Ok(MapToDto(request));
    }

    [HttpPost("engine")]
    public async Task<IActionResult> CreateEngineBuild(
        [FromBody] CreateBuildRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (dto == null) return BadRequest(ApiResponse.Error("Request is empty."));
        var customer = await _userManager.GetUserAsync(User);
        if (customer is null) return Unauthorized();

        try
        {
            var request = await _submissionService.QueueAsync(
                dto,
                new BuildCustomerIdentity(customer.Id, customer.Email, GetFullName(customer)),
                cancellationToken);
            await _buildRepo.SaveChangesAsync(cancellationToken);
            return Ok(MapToDto(request));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse.Error(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiResponse.Error(exception.Message));
        }
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
        var build = await _buildRepo.GetByIdAsync(id);
        if (build == null) return NotFound(ApiResponse.NotFound("Build"));

        if (string.Equals(newStatus, WorkOrderStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Error("Use the completion checkout endpoint to complete a build."));

        var canonicalStatus = new[] { WorkOrderStatuses.Pending, WorkOrderStatuses.Confirmed, WorkOrderStatuses.Cancelled }
            .SingleOrDefault(value => string.Equals(value, newStatus?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (canonicalStatus is null) return BadRequest(ApiResponse.Error("Unsupported build status."));
        var transitionError = WorkOrderRules.ValidateStatusTransition(build.Status, canonicalStatus);
        if (transitionError is not null) return Conflict(ApiResponse.Error(transitionError));

        build.Status = canonicalStatus;
        await _buildRepo.SaveChangesAsync();
        return Ok(new { message = "Status updated" });
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
        catch (InvalidOperationException exception)
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
        InvoiceNumber = build.Transaction?.InvoiceNumber
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
