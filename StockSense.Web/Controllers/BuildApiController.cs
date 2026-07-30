using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

namespace StockSense.Web.Controllers;

[ApiController]
[Route("api/build")]
public sealed class BuildApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICompatibilityEngine _compatibility;
    private readonly IPerformanceCalculator _performance;
    private readonly IBuildRequestSubmissionService _submissionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public BuildApiController(
        ApplicationDbContext context,
        ICompatibilityEngine compatibility,
        IPerformanceCalculator performance,
        IBuildRequestSubmissionService submissionService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _compatibility = compatibility;
        _performance = performance;
        _submissionService = submissionService;
        _userManager = userManager;
    }

    [HttpGet("bike-models")]
    public async Task<ActionResult<List<BikeModel>>> GetBikeModels(CancellationToken cancellationToken) =>
        Ok(await _context.BikeModels
            .AsNoTracking()
            .Where(model => model.IsActive)
            .OrderBy(model => model.Brand)
            .ThenBy(model => model.Model)
            .ToListAsync(cancellationToken));

    [HttpGet("bike-models/grouped")]
    public async Task<ActionResult<Dictionary<string, List<BikeModel>>>> GetBikeModelsGrouped(
        CancellationToken cancellationToken)
    {
        var models = await _context.BikeModels
            .AsNoTracking()
            .Where(model => model.IsActive)
            .OrderBy(model => model.Brand)
            .ThenBy(model => model.Model)
            .ToListAsync(cancellationToken);
        return Ok(models.GroupBy(model => model.Brand)
            .ToDictionary(group => group.Key, group => group.ToList()));
    }

    [HttpGet("stages/all")]
    public async Task<ActionResult<List<UpgradeStage>>> GetAllStages(CancellationToken cancellationToken) =>
        Ok(await _context.UpgradeStages
            .AsNoTracking()
            .Where(stage => stage.IsActive)
            .OrderBy(stage => stage.BikeModelId)
            .ThenBy(stage => stage.StageNumber)
            .ToListAsync(cancellationToken));

    [HttpGet("bike-models/{id:int}/stages")]
    public async Task<ActionResult<List<UpgradeStage>>> GetStagesForBike(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await _context.UpgradeStages
            .AsNoTracking()
            .Where(stage => stage.BikeModelId == id && stage.IsActive && stage.IsGuidedPath)
            .OrderBy(stage => stage.StageNumber)
            .ToListAsync(cancellationToken));

    [HttpGet("categories")]
    public async Task<ActionResult<List<UpgradeCategory>>> GetCategories(
        [FromQuery] int? bikeModelId,
        CancellationToken cancellationToken)
    {
        var categories = await _context.UpgradeCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.DisplayOrder)
            .ToListAsync(cancellationToken);

        if (bikeModelId.HasValue)
        {
            foreach (var category in categories)
            {
                category.PartCount = (await _compatibility.GetCompatiblePartsAsync(
                    bikeModelId.Value,
                    category.Id,
                    [],
                    cancellationToken)).Count;
            }
        }

        return Ok(categories);
    }

    [HttpGet("parts/all")]
    public async Task<ActionResult<List<UpgradePart>>> GetAllParts(CancellationToken cancellationToken) =>
        Ok(await _context.UpgradeParts
            .AsNoTracking()
            .Include(part => part.Category)
            .Include(part => part.Product)
            .Where(part => part.IsActive && part.Category.IsActive)
            .OrderBy(part => part.Category.DisplayOrder)
            .ThenBy(part => part.Product.Name)
            .ToListAsync(cancellationToken));

    [HttpGet("parts")]
    public async Task<ActionResult<List<UpgradePart>>> GetParts(
        [FromQuery] int bikeModelId,
        [FromQuery] int categoryId,
        [FromQuery] List<int>? selectedPartIds,
        CancellationToken cancellationToken)
    {
        if (bikeModelId <= 0 || categoryId <= 0) return BadRequest("Select a motorcycle and category.");
        return Ok(await _compatibility.GetCompatiblePartsAsync(
            bikeModelId,
            categoryId,
            selectedPartIds ?? [],
            cancellationToken));
    }

    [HttpGet("parts/{id:int}/price-history")]
    public async Task<ActionResult<PartPriceHistoryDto>> GetPartPriceHistory(
        int id,
        CancellationToken cancellationToken)
    {
        var part = await _context.UpgradeParts
            .AsNoTracking()
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == id && item.IsActive, cancellationToken);
        if (part is null) return NotFound();

        var productId = part.ProductId.ToString();
        var sales = await _context.SalesHistory
            .AsNoTracking()
            .Where(sale => sale.ProductID == productId && sale.UnitPrice > 0)
            .Select(sale => new { sale.Year, sale.MonthNum, sale.UnitPrice })
            .ToListAsync(cancellationToken);
        var points = sales
            .GroupBy(sale => new { Year = (int)sale.Year, Month = (int)sale.MonthNum })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.Month)
            .Select(group => new PartPricePointDto
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                MedianUnitPrice = (decimal)Median(group.Select(sale => (double)sale.UnitPrice)),
                Transactions = group.Count()
            })
            .TakeLast(12)
            .ToList();

        var trend = "Insufficient history";
        if (points.Count >= 2)
        {
            var change = points[^1].MedianUnitPrice - points[0].MedianUnitPrice;
            trend = Math.Abs(change) < 0.01m
                ? "Stable"
                : change > 0 ? $"Up PHP {change:N2}" : $"Down PHP {Math.Abs(change):N2}";
        }

        return Ok(new PartPriceHistoryDto
        {
            UpgradePartId = part.Id,
            ProductId = part.ProductId,
            ProductName = part.Product.Name,
            CurrentPrice = part.ListPrice > 0 ? part.ListPrice : part.Product.Price,
            Trend = trend,
            Points = points
        });
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ValidationResult>> ValidateBuild(
        [FromBody] ValidateBuildRequest request,
        CancellationToken cancellationToken)
    {
        if (request.BikeModelId <= 0) return BadRequest("Invalid motorcycle model.");
        return Ok(await _compatibility.ValidateBuildAsync(
            request.BikeModelId,
            request.PartIds,
            request.StageId,
            cancellationToken));
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<BuildProjection>> CalculateBuild(
        [FromBody] CalculateBuildRequest request,
        CancellationToken cancellationToken)
    {
        if (request.BikeModelId <= 0) return BadRequest("Invalid motorcycle model.");
        try
        {
            return Ok(await _performance.CalculateAsync(
                request.BikeModelId,
                request.PartIds,
                cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("calculate-stage")]
    public async Task<ActionResult<BuildProjection>> CalculateStage(
        [FromBody] CalculateStageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _performance.CalculateForStageAsync(
                request.BikeModelId,
                request.StageId,
                request.CustomPartIds,
                cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }


    [Authorize]
    [HttpPost("draft")]
    public async Task<ActionResult<CustomerBuild>> SaveDraft(
        [FromBody] CustomerBuild request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var draft = new CustomerBuild
        {
            UserId = userId,
            BikeModelId = request.BikeModelId,
            UpgradeStageId = request.UpgradeStageId,
            SelectedPartIdsJson = request.SelectedPartIdsJson,
            Status = EngineBuildStatuses.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await RecalculateDraftAsync(draft, cancellationToken);
        _context.CustomerBuilds.Add(draft);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(draft);
    }

    [Authorize]
    [HttpPut("draft/{id:int}")]
    public async Task<ActionResult<CustomerBuild>> UpdateDraft(
        int id,
        [FromBody] CustomerBuild request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var draft = await _context.CustomerBuilds.FirstOrDefaultAsync(
            build =>
                build.Id == id &&
                build.UserId == userId &&
                build.Status == EngineBuildStatuses.Draft,
            cancellationToken);
        if (draft is null) return NotFound();

        draft.BikeModelId = request.BikeModelId;
        draft.UpgradeStageId = request.UpgradeStageId;
        draft.SelectedPartIdsJson = request.SelectedPartIdsJson;
        draft.UpdatedAt = DateTime.UtcNow;
        await RecalculateDraftAsync(draft, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(draft);
    }

    [Authorize]
    [HttpGet("drafts")]
    public async Task<ActionResult<List<CustomerBuild>>> GetDrafts(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(await _context.CustomerBuilds
            .AsNoTracking()
            .Where(build => build.UserId == userId && build.Status == EngineBuildStatuses.Draft)
            .OrderByDescending(build => build.UpdatedAt)
            .ToListAsync(cancellationToken));
    }

    [Authorize]
    [HttpGet("draft/{id:int}")]
    public async Task<ActionResult<CustomerBuild>> GetDraft(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var draft = await _context.CustomerBuilds
            .AsNoTracking()
            .FirstOrDefaultAsync(build => build.Id == id && build.UserId == userId, cancellationToken);
        return draft is null ? NotFound() : Ok(draft);
    }

    [Authorize]
    [HttpDelete("draft/{id:int}")]
    public async Task<IActionResult> DeleteDraft(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var draft = await _context.CustomerBuilds.FirstOrDefaultAsync(
            build =>
                build.Id == id &&
                build.UserId == userId &&
                build.Status == EngineBuildStatuses.Draft,
            cancellationToken);
        if (draft is null) return NotFound();

        _context.CustomerBuilds.Remove(draft);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("submit")]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitEngineBuildRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _userManager.GetUserAsync(User);
        if (customer is null) return Unauthorized();

        var draft = await _context.CustomerBuilds
            .Include(build => build.BikeModel)
            .FirstOrDefaultAsync(
                build =>
                    build.Id == request.DraftId &&
                    build.UserId == customer.Id &&
                    build.Status == EngineBuildStatuses.Draft,
                cancellationToken);
        if (draft is null) return NotFound("Draft not found.");
        if (!draft.BikeModelId.HasValue) return BadRequest("Select a motorcycle.");

        var partIds = DeserializePartIds(draft.SelectedPartIdsJson);
        if (partIds.Count == 0) return BadRequest("Select at least one engine part.");

        var validation = await _compatibility.ValidateBuildAsync(
            draft.BikeModelId.Value,
            partIds,
            draft.UpgradeStageId,
            cancellationToken);
        if (!validation.IsValid) return BadRequest(new { errors = validation.Errors });

        var parts = await _context.UpgradeParts
            .AsNoTracking()
            .Include(part => part.Product)
            .ThenInclude(product => product.Supplier)
            .Where(part => partIds.Contains(part.Id) && part.IsActive)
            .ToListAsync(cancellationToken);
        var outOfStock = parts.Where(part => part.Product.CurrentStock <= 0)
            .Select(part => part.Product.Name)
            .ToArray();
        if (outOfStock.Length > 0)
            return Conflict(new
            {
                error = $"Out-of-stock parts are estimate-only: {string.Join(", ", outOfStock)}."
            });

        var projection = await _performance.CalculateAsync(
            draft.BikeModelId.Value,
            partIds,
            cancellationToken);
        var selectedProducts = parts.Select(part => new ProductDto(
            part.Product.Id,
            part.Product.Name,
            part.Product.Category,
            part.Product.Brand,
            part.ListPrice > 0 ? part.ListPrice : part.Product.Price,
            part.Product.CurrentStock,
            part.Product.ReorderTarget,
            part.Product.SupplierId ?? 0,
            part.Product.Supplier?.Name ?? string.Empty,
            part.Product.ImageUrl,
            part.Product.Barcode,
            part.Product.UnitCost)).ToList();
        selectedProducts.Add(new ProductDto(
            -999,
            "TYPE_ENGINE",
            "SYSTEM_METADATA",
            "Build Engine",
            ImageUrl: GetDisplayName(customer)));

        var buildName = string.IsNullOrWhiteSpace(request.BuildName)
            ? $"{draft.BikeModel?.DisplayName ?? "Motorcycle"} Engine Build"
            : request.BuildName.Trim();
        var workOrder = await _submissionService.QueueAsync(
            new CreateBuildRequestDto
            {
                CustomerName = GetDisplayName(customer),
                BuildName = buildName,
                SelectedPartsJson = JsonSerializer.Serialize(selectedProducts),
                TotalPrice = projection.TotalCost
            },
            new BuildCustomerIdentity(customer.Id, customer.Email, GetDisplayName(customer)),
            cancellationToken);

        draft.Status = EngineBuildStatuses.Submitted;
        draft.BuildRequest = workOrder;
        draft.CurrentCC = projection.FinalCC;
        draft.ProjectedHP = projection.FinalHP;
        draft.ProjectedTorque = projection.FinalTorque;
        draft.ReliabilityScore = projection.ReliabilityScore;
        draft.TotalPartsCost = projection.TotalPartsCost;
        draft.EstimatedLaborCost = projection.EstimatedLaborCost;
        draft.ValidationErrorsJson = JsonSerializer.Serialize(validation.Errors);
        draft.MissingRequirementsJson = JsonSerializer.Serialize(validation.MissingRequirements);
        draft.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { buildRequestId = workOrder.Id, message = "Build submitted successfully." });
    }

    private async Task RecalculateDraftAsync(CustomerBuild draft, CancellationToken cancellationToken)
    {
        if (!draft.BikeModelId.HasValue) return;
        var partIds = DeserializePartIds(draft.SelectedPartIdsJson);
        if (partIds.Count == 0) return;

        var validation = await _compatibility.ValidateBuildAsync(
            draft.BikeModelId.Value,
            partIds,
            draft.UpgradeStageId,
            cancellationToken);
        var projection = await _performance.CalculateAsync(
            draft.BikeModelId.Value,
            partIds,
            cancellationToken);

        draft.CurrentCC = projection.FinalCC;
        draft.ProjectedHP = projection.FinalHP;
        draft.ProjectedTorque = projection.FinalTorque;
        draft.ReliabilityScore = projection.ReliabilityScore;
        draft.TotalPartsCost = projection.TotalPartsCost;
        draft.EstimatedLaborCost = projection.EstimatedLaborCost;
        draft.ValidationErrorsJson = JsonSerializer.Serialize(validation.Errors);
        draft.MissingRequirementsJson = JsonSerializer.Serialize(validation.MissingRequirements);
    }

    private static List<int> DeserializePartIds(string? json)
    {
        try { return JsonSerializer.Deserialize<List<int>>(json ?? "[]")?.Distinct().ToList() ?? []; }
        catch (JsonException) { return []; }
    }

    private static string GetDisplayName(ApplicationUser user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName)
            ? user.Email?.Split('@')[0] ?? "Customer"
            : fullName;
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(value => value).ToArray();
        if (sorted.Length == 0) return 0;
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 0
            ? (sorted[middle - 1] + sorted[middle]) / 2d
            : sorted[middle];
    }
}
