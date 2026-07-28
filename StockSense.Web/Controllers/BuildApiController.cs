using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Services;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;

namespace StockSense.Web.Controllers
{
    [ApiController]
    [Route("api/build")]
    public class BuildApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICompatibilityEngine _compatibility;
        private readonly IPerformanceCalculator _performance;

        public BuildApiController(
            ApplicationDbContext context,
            ICompatibilityEngine compatibility,
            IPerformanceCalculator performance)
        {
            _context = context;
            _compatibility = compatibility;
            _performance = performance;
        }

        // ============================================
        // BIKE MODELS & STAGES (Steps 1-2)
        // ============================================

        [HttpGet("bike-models")]
        public async Task<ActionResult<List<BikeModel>>> GetBikeModels()
        {
            await EnsureBuildCatalogSeededAsync();

            var models = await _context.BikeModels
                .Where(b => b.IsActive)
                .OrderBy(b => b.Brand)
                .ThenBy(b => b.Model)
                .ToListAsync();

            return Ok(models);
        }

        [HttpGet("bike-models/grouped")]
        public async Task<ActionResult<Dictionary<string, List<BikeModel>>>> GetBikeModelsGrouped()
        {
            await EnsureBuildCatalogSeededAsync();

            var models = await _context.BikeModels
                .Where(b => b.IsActive)
                .OrderBy(b => b.Brand)
                .ThenBy(b => b.Model)
                .ToListAsync();

            var grouped = models
                .GroupBy(m => m.Brand)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(m => m.Model).ToList()
                );

            return Ok(grouped);
        }

        private async Task EnsureBuildCatalogSeededAsync()
        {
            if (!await _context.BikeModels.AnyAsync(b => b.IsActive))
            {
                await DevelopmentCatalogSeeder.SeedScooterUpgradeCatalogAsync(_context);
            }
        }

        [HttpGet("stages/all")]
        public async Task<ActionResult<List<UpgradeStage>>> GetAllStages()
        {
            var stages = await _context.UpgradeStages
                .Where(s => s.IsActive)
                .OrderBy(s => s.BikeModelId)
                .ThenBy(s => s.StageNumber)
                .ToListAsync();

            return Ok(stages);
        }

        [HttpGet("parts/{id:int}/price-history")]
        public async Task<ActionResult<PartPriceHistoryDto>> GetPartPriceHistory(int id)
        {
            var part = await _context.UpgradeParts.AsNoTracking()
                .Include(item => item.Product)
                .FirstOrDefaultAsync(item => item.Id == id && item.IsActive);
            if (part == null) return NotFound();

            var productId = part.ProductId.ToString();
            var raw = await _context.SalesHistory.AsNoTracking()
                .Where(sale => sale.ProductID == productId && sale.UnitPrice > 0)
                .Select(sale => new { sale.Year, sale.MonthNum, sale.UnitPrice })
                .ToListAsync();
            var points = raw
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
                var difference = points[^1].MedianUnitPrice - points[0].MedianUnitPrice;
                trend = Math.Abs(difference) < 0.01m ? "Stable"
                    : difference > 0 ? $"Up ₱{difference:N2}" : $"Down ₱{Math.Abs(difference):N2}";
            }

            return Ok(new PartPriceHistoryDto
            {
                UpgradePartId = part.Id,
                ProductId = part.ProductId,
                ProductName = part.Product.Name,
                CurrentPrice = part.ListPrice,
                Trend = trend,
                Points = points
            });
        }

        [HttpGet("bike-models/{id}/stages")]
        public async Task<ActionResult<List<UpgradeStage>>> GetStagesForBike(int id)
        {
            var stages = await _context.UpgradeStages
                .Where(s => s.BikeModelId == id && s.IsActive && s.IsGuidedPath)
                .OrderBy(s => s.StageNumber)
                .ToListAsync();

            return Ok(stages);
        }

        // ============================================
        // CATEGORIES & PARTS (Step 3)
        // ============================================

        [HttpGet("categories")]
        public async Task<ActionResult<List<UpgradeCategory>>> GetCategories([FromQuery] int? bikeModelId = null)
        {
            var query = _context.UpgradeCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder);

            var categories = await query.ToListAsync();

            // If bike model specified, add part counts
            if (bikeModelId.HasValue)
            {
                var allParts = await _context.UpgradeParts
                    .Where(p => p.IsActive)
                    .ToListAsync();

                var partCounts = allParts
                    .Where(p =>
                    {
                        var ids = DeserializePartIds(p.CompatibleModelsJson);
                        return ids.Contains(bikeModelId.Value);
                    })
                    .GroupBy(p => p.UpgradeCategoryId)
                    .ToDictionary(g => g.Key, g => g.Count());

                foreach (var cat in categories)
                {
                    cat.PartCount = partCounts.GetValueOrDefault(cat.Id, 0);
                }
            }

            return Ok(categories);
        }

        [HttpGet("parts/all")]
        public async Task<ActionResult<List<UpgradePart>>> GetAllParts()
        {
            var parts = await _context.UpgradeParts
                .Include(p => p.Category)
                .Include(p => p.Product)
                .Where(p => p.IsActive && p.Product != null && p.Category != null && p.Category.IsActive)
                .ToListAsync();

            return Ok(parts);
        }

        [HttpGet("parts")]
        public async Task<ActionResult<List<UpgradePart>>> GetParts(
            [FromQuery] int bikeModelId,
            [FromQuery] int categoryId,
            [FromQuery] List<int>? selectedPartIds = null)
        {
            var parts = await _compatibility.GetCompatiblePartsAsync(bikeModelId, categoryId, selectedPartIds ?? new List<int>());
            return Ok(parts);
        }

        // ============================================
        // VALIDATION & CALCULATION (Real-time)
        // ============================================

        [HttpPost("validate")]
        public async Task<ActionResult<ValidationResult>> ValidateBuild(
            [FromBody] ValidateBuildRequest request)
        {
            if (request.BikeModelId <= 0) return BadRequest("Invalid BikeModelId");
            var result = await _compatibility.ValidateBuildAsync(request.BikeModelId, request.PartIds, request.StageId);
            return Ok(result);
        }

        [HttpPost("calculate")]
        public async Task<ActionResult<BuildProjection>> CalculateBuild(
            [FromBody] CalculateBuildRequest request)
        {
            if (request.BikeModelId <= 0) return BadRequest("Invalid BikeModelId");
            var projection = await _performance.CalculateAsync(request.BikeModelId, request.PartIds);
            return Ok(projection);
        }

        [HttpPost("calculate-stage")]
        public async Task<ActionResult<BuildProjection>> CalculateForStage(
            [FromBody] CalculateStageRequest request)
        {
            if (request.BikeModelId <= 0) return BadRequest("Invalid BikeModelId");
            var projection = await _performance.CalculateForStageAsync(request.BikeModelId, request.StageId, request.CustomPartIds);
            return Ok(projection);
        }

        [HttpPost("maintenance")]
        public async Task<ActionResult<MaintenanceProjection>> GetMaintenance(
            [FromBody] MaintenanceRequest request)
        {
            if (request.BikeModelId <= 0) return BadRequest("Invalid BikeModelId");
            var maintenance = await _performance.CalculateMaintenanceAsync(request.BikeModelId, request.PartIds);
            return Ok(maintenance);
        }

        // ============================================
        // CUSTOMER BUILDS (Drafts & Submissions)
        // ============================================

        [HttpPost("draft")]
        [Authorize]
        public async Task<ActionResult<CustomerBuild>> SaveDraft([FromBody] CustomerBuild draft)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            draft.UserId = userId;
            if (string.IsNullOrEmpty(draft.Status)) draft.Status = "Draft";
            draft.UpdatedAt = DateTime.UtcNow;
            draft.CreatedAt = DateTime.UtcNow;

            // Run validation and calculation before saving
            if (draft.SelectedPartIdsJson != "[]" && draft.BikeModelId.HasValue)
            {
                var partIds = DeserializePartIds(draft.SelectedPartIdsJson);

                if (partIds.Any() && draft.BikeModelId.HasValue)
                {
                    var validation = await _compatibility.ValidateBuildAsync(draft.BikeModelId.Value, partIds, draft.UpgradeStageId);
                    draft.ValidationWarningsJson = JsonSerializer.Serialize(validation.Warnings);
                    draft.ValidationErrorsJson = JsonSerializer.Serialize(validation.Errors);
                    draft.MissingRequirementsJson = JsonSerializer.Serialize(validation.MissingRequirements);

                    var projection = await _performance.CalculateAsync(draft.BikeModelId.Value, partIds);
                    draft.CurrentCC = projection.FinalCC;
                    draft.ProjectedHP = projection.FinalHP;
                    draft.ProjectedTorque = projection.FinalTorque;
                    draft.ReliabilityScore = projection.ReliabilityScore;
                    draft.TotalPartsCost = projection.TotalPartsCost;
                    draft.EstimatedLaborCost = projection.EstimatedLaborCost;
                    draft.MaintenanceProjectionJson = JsonSerializer.Serialize(projection.Maintenance);
                }
            }

            _context.CustomerBuilds.Add(draft);
            await _context.SaveChangesAsync();

            return Ok(draft);
        }

        [HttpPut("draft/{id}")]
        [Authorize]
        public async Task<ActionResult<CustomerBuild>> UpdateDraft(int id, [FromBody] CustomerBuild updated)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var draft = await _context.CustomerBuilds
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (draft == null)
                return NotFound();

            draft.BikeModelId = updated.BikeModelId;
            draft.UpgradeStageId = updated.UpgradeStageId;
            draft.SelectedPartIdsJson = updated.SelectedPartIdsJson;
            draft.Status = "Draft";
            draft.UpdatedAt = DateTime.UtcNow;

            // Recalculate
            if (!string.IsNullOrEmpty(draft.SelectedPartIdsJson) && draft.SelectedPartIdsJson != "[]" && draft.BikeModelId.HasValue)
            {
                var partIds = DeserializePartIds(draft.SelectedPartIdsJson);
                
                var validation = await _compatibility.ValidateBuildAsync(draft.BikeModelId.Value, partIds, draft.UpgradeStageId);
                draft.ValidationWarningsJson = JsonSerializer.Serialize(validation.Warnings);
                draft.ValidationErrorsJson = JsonSerializer.Serialize(validation.Errors);
                draft.MissingRequirementsJson = JsonSerializer.Serialize(validation.MissingRequirements);

                var projection = await _performance.CalculateAsync(draft.BikeModelId.Value, partIds);
                draft.CurrentCC = projection.FinalCC;
                draft.ProjectedHP = projection.FinalHP;
                draft.ProjectedTorque = projection.FinalTorque;
                draft.ReliabilityScore = projection.ReliabilityScore;
                draft.TotalPartsCost = projection.TotalPartsCost;
                draft.EstimatedLaborCost = projection.EstimatedLaborCost;
                draft.MaintenanceProjectionJson = JsonSerializer.Serialize(projection.Maintenance);
            }

            await _context.SaveChangesAsync();
            return Ok(draft);
        }

        [HttpGet("drafts")]
        [Authorize]
        public async Task<ActionResult<List<CustomerBuild>>> GetMyDrafts()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var drafts = await _context.CustomerBuilds
                .Where(b => b.UserId == userId && b.Status == "Draft")
                .OrderByDescending(b => b.UpdatedAt)
                .ToListAsync();

            return Ok(drafts);
        }

        [HttpGet("draft/{id}")]
        [Authorize]
        public async Task<ActionResult<CustomerBuild>> GetDraft(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var draft = await _context.CustomerBuilds
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (draft == null)
                return NotFound();

            return Ok(draft);
        }

        [HttpPost("submit")]
        [Authorize]
        public async Task<ActionResult<BuildRequest>> SubmitBuild([FromBody] SubmitBuildRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var draft = await _context.CustomerBuilds
                .FirstOrDefaultAsync(b => b.Id == request.DraftId && b.UserId == userId);

            if (draft == null)
                return NotFound("Draft not found");

            if (draft.Status != "Draft")
                return BadRequest("Draft already submitted");

            if (!draft.BikeModelId.HasValue)
                return BadRequest("Build has no bike model selected");

            // Final validation
            var partIds = DeserializePartIds(draft.SelectedPartIdsJson);
            var validation = await _compatibility.ValidateBuildAsync(draft.BikeModelId.Value, partIds, draft.UpgradeStageId);

            if (!validation.IsValid)
            {
                return BadRequest(new { errors = validation.Errors });
            }

            // Create BuildRequest (existing system)
            var upgradeParts = await _context.UpgradeParts
                .Include(part => part.Product)
                .Where(part => partIds.Contains(part.Id))
                .ToListAsync();
            var parts = upgradeParts
                .Where(part => part.Product != null)
                .Select(part => part.Product!)
                .ToList();

            var buildRequest = new BuildRequest
            {
                CustomerName = User.Identity?.Name ?? userId,
                BuildName = request.BuildName ?? $"{draft.BikeModel?.DisplayName} Build",
                SelectedPartsJson = JsonSerializer.Serialize(parts),
                TotalPrice = draft.TotalPartsCost + draft.EstimatedLaborCost,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.BuildRequests.Add(buildRequest);

            // Update draft
            draft.Status = "Submitted";
            draft.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { buildRequestId = buildRequest.Id, message = "Build submitted successfully" });
        }

        [HttpGet("my-builds")]
        [Authorize]
        public async Task<ActionResult<List<CustomerBuild>>> GetMyBuilds()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var builds = await _context.CustomerBuilds
                .Where(b => b.UserId == userId && b.Status != "Draft")
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return Ok(builds);
        }

        [HttpGet("my-builds/enriched")]
        [Authorize]
        public async Task<ActionResult<List<BuildSummaryDto>>> GetMyBuildsEnriched()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var builds = await _context.CustomerBuilds
                .Include(b => b.BikeModel)
                .Include(b => b.UpgradeStage)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var allPartIds = builds
                .SelectMany(b => DeserializePartIds(b.SelectedPartIdsJson))
                .Distinct()
                .ToList();

            var parts = await _context.UpgradeParts
                .Include(p => p.Product)
                .Include(p => p.Category)
                .Where(p => allPartIds.Contains(p.Id))
                .ToListAsync();

            var results = builds.Select(b =>
            {
                var partIds = DeserializePartIds(b.SelectedPartIdsJson);
                var buildParts = parts.Where(p => partIds.Contains(p.Id)).Select(p => new PartSummaryDto
                {
                    Id = p.Id,
                    Name = p.Product?.Name ?? "Unknown",
                    Brand = p.Product?.Brand ?? "",
                    CategoryName = p.Category?.Name ?? "",
                    CCGain = p.CCGain,
                    HPGain = p.HPGain,
                    TorqueGain = p.TorqueGain,
                    ReliabilityImpact = p.ReliabilityImpact,
                    ListPrice = p.ListPrice
                }).ToList();

                MaintenanceProjection? maint = null;
                try { maint = JsonSerializer.Deserialize<MaintenanceProjection>(b.MaintenanceProjectionJson); } catch { }

                return new BuildSummaryDto
                {
                    Id = b.Id,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt,
                    BikeBrand = b.BikeModel?.Brand ?? "",
                    BikeModel = b.BikeModel?.Model ?? "",
                    BikeYearStart = b.BikeModel?.YearStart ?? 0,
                    BikeYearEnd = b.BikeModel?.YearEnd ?? 0,
                    BaseCC = b.BikeModel?.BaseCC ?? 0,
                    BaseHP = b.BikeModel?.BaseHP ?? 0,
                    BaseTorque = b.BikeModel?.BaseTorque ?? 0,
                    EngineCode = b.BikeModel?.EngineCode ?? "",
                    StageName = b.UpgradeStage?.Name,
                    StageNumber = b.UpgradeStage?.StageNumber,
                    CurrentCC = b.CurrentCC,
                    ProjectedHP = b.ProjectedHP,
                    ProjectedTorque = b.ProjectedTorque,
                    ReliabilityScore = b.ReliabilityScore,
                    TotalPartsCost = b.TotalPartsCost,
                    EstimatedLaborCost = b.EstimatedLaborCost,
                    Parts = buildParts,
                    PartCount = partIds.Count,
                    MaintenanceTier = maint?.MaintenanceTier,
                    ValidationWarnings = DeserializeStringList(b.ValidationWarningsJson),
                    ValidationErrors = DeserializeStringList(b.ValidationErrorsJson)
                };
            }).ToList();

            return Ok(results);
        }

        [HttpDelete("draft/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteDraft(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var draft = await _context.CustomerBuilds
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (draft == null)
                return NotFound();

            if (draft.Status != "Draft")
                return BadRequest("Only drafts can be deleted");

            _context.CustomerBuilds.Remove(draft);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static List<int> DeserializePartIds(string json)
        {
            try { return JsonSerializer.Deserialize<List<int>>(json) ?? new(); }
            catch { return new(); }
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

        private static List<string> DeserializeStringList(string json)
        {
            try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
            catch { return new(); }
        }

        // ============================================
        // ADMIN ENDPOINTS
        // ============================================

        [HttpGet("admin/customer-builds")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<List<CustomerBuild>>> GetAllCustomerBuilds(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.CustomerBuilds
                .Include(b => b.BikeModel)
                .Include(b => b.UpgradeStage)
                .Where(b => b.Status != "Draft");

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status == status);

            var total = await query.CountAsync();
            var builds = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { builds, total, page, pageSize });
        }

        [HttpPut("admin/customer-builds/{id}/status")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult> UpdateBuildStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var build = await _context.CustomerBuilds.FindAsync(id);
            if (build == null) return NotFound();

            build.Status = request.Status;
            build.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // ============================================
        // ADMIN: PARTS CRUD
        // ============================================

        [HttpGet("admin/parts")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<List<UpgradePart>>> GetAllPartsAdmin()
        {
            var parts = await _context.UpgradeParts
                .Include(p => p.Category)
                .Include(p => p.Product)
                .OrderBy(p => p.UpgradeCategoryId)
                .ThenBy(p => p.Product!.Name)
                .ToListAsync();
            return Ok(parts);
        }

        [HttpGet("admin/parts/{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<UpgradePart>> GetPart(int id)
        {
            var part = await _context.UpgradeParts
                .Include(p => p.Category)
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (part == null) return NotFound();
            return Ok(part);
        }

        [HttpPost("admin/parts")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<UpgradePart>> CreatePart([FromBody] AdminPartDto dto)
        {
            var product = new Product
            {
                Name = dto.ProductName,
                Brand = dto.Brand,
                Category = "Racing Parts",
                Price = dto.ProductPrice,
                ImageUrl = "https://placehold.co/300x200",
                CurrentStock = 10,
                ReorderTarget = 3,
                SupplierId = 1
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var part = new UpgradePart
            {
                ProductId = product.Id,
                UpgradeCategoryId = dto.UpgradeCategoryId,
                ListPrice = dto.ListPrice,
                EstimatedLaborHours = dto.EstimatedLaborHours,
                CCGain = dto.CCGain,
                HPGain = dto.HPGain,
                TorqueGain = dto.TorqueGain,
                ReliabilityImpact = dto.ReliabilityImpact,
                CompatibleModelsJson = dto.CompatibleModelsJson ?? "[]",
                RequiredForStagesJson = dto.RequiredForStagesJson ?? "[]",
                IsActive = dto.IsActive
            };
            _context.UpgradeParts.Add(part);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPart), new { id = part.Id }, part);
        }

        [HttpPut("admin/parts/{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult> UpdatePart(int id, [FromBody] AdminPartDto dto)
        {
            var part = await _context.UpgradeParts
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (part == null) return NotFound();

            part.UpgradeCategoryId = dto.UpgradeCategoryId;
            part.ListPrice = dto.ListPrice;
            part.EstimatedLaborHours = dto.EstimatedLaborHours;
            part.CCGain = dto.CCGain;
            part.HPGain = dto.HPGain;
            part.TorqueGain = dto.TorqueGain;
            part.ReliabilityImpact = dto.ReliabilityImpact;
            part.CompatibleModelsJson = dto.CompatibleModelsJson ?? "[]";
            part.RequiredForStagesJson = dto.RequiredForStagesJson ?? "[]";
            part.IsActive = dto.IsActive;

            if (part.Product != null)
            {
                part.Product.Name = dto.ProductName;
                part.Product.Brand = dto.Brand;
                part.Product.Price = dto.ProductPrice;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("admin/parts/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeletePart(int id)
        {
            var part = await _context.UpgradeParts
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (part == null) return NotFound();

            _context.UpgradeParts.Remove(part);
            if (part.Product != null)
                _context.Products.Remove(part.Product);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }

    // Request DTOs
    public class ValidateBuildRequest
    {
        public int BikeModelId { get; set; }
        public List<int> PartIds { get; set; } = new();
        public int? StageId { get; set; }
    }

    public class CalculateBuildRequest
    {
        public int BikeModelId { get; set; }
        public List<int> PartIds { get; set; } = new();
    }

    public class CalculateStageRequest
    {
        public int BikeModelId { get; set; }
        public int StageId { get; set; }
        public List<int>? CustomPartIds { get; set; }
    }

    public class MaintenanceRequest
    {
        public int BikeModelId { get; set; }
        public List<int> PartIds { get; set; } = new();
    }

    public class SubmitBuildRequest
    {
        public int DraftId { get; set; }
        public string? BuildName { get; set; }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class AdminPartDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal ProductPrice { get; set; }
        public int UpgradeCategoryId { get; set; }
        public decimal ListPrice { get; set; }
        public decimal EstimatedLaborHours { get; set; }
        public int CCGain { get; set; }
        public int HPGain { get; set; }
        public int TorqueGain { get; set; }
        public int ReliabilityImpact { get; set; }
        public string? CompatibleModelsJson { get; set; }
        public string? RequiredForStagesJson { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
