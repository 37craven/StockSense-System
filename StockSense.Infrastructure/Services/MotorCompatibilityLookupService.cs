using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

namespace StockSense.Infrastructure.Services;

public sealed class MotorCompatibilityLookupService(ApplicationDbContext context)
    : IMotorCompatibilityLookupService
{
    public async Task<IReadOnlyList<MotorCompatibilityDto>> FindExactAsync(
        MotorCompatibilityLookupQuery query,
        CancellationToken cancellationToken = default)
    {
        var manufacturer = query.Manufacturer.ToUpperInvariant();
        var modelName = query.ModelName.ToUpperInvariant();
        var versionName = query.VersionName.ToUpperInvariant();
        var matches = await context.MotorCompatibilities
            .AsNoTracking()
            .Where(compatibility =>
                compatibility.Manufacturer.ToUpper() == manufacturer
                && compatibility.ModelName.ToUpper() == modelName
                && compatibility.VersionName.ToUpper() == versionName
                && compatibility.YearStart <= query.Year
                && (compatibility.YearEnd == null || compatibility.YearEnd >= query.Year))
            .Include(compatibility => compatibility.ProductMappings)
                .ThenInclude(mapping => mapping.Product)
            .OrderByDescending(compatibility => compatibility.YearStart)
            .ThenBy(compatibility => compatibility.YearEnd)
            .ToListAsync(cancellationToken);

        return matches.Select(ToDto).ToList();
    }

    private static MotorCompatibilityDto ToDto(MotorCompatibility compatibility) => new(
        compatibility.CompatibilityId,
        compatibility.Manufacturer,
        compatibility.ModelName,
        compatibility.VersionName,
        compatibility.YearStart,
        compatibility.YearEnd,
        compatibility.EngineOilSpec,
        compatibility.GearOilSpec,
        compatibility.CoolantSpec,
        compatibility.SparkPlugSpec,
        compatibility.FuelFilterSpec,
        compatibility.DriveBeltSpec,
        compatibility.FlyBallWeight,
        compatibility.CenterSpringSpec,
        compatibility.BrakePadFront,
        compatibility.BrakePadRear,
        compatibility.BrakeShoeRear,
        compatibility.AirFilterSpec,
        compatibility.ProductMappings
            .OrderBy(mapping => mapping.PartFunction)
            .ThenBy(mapping => mapping.Product.Name)
            .Select(mapping => new CompatibleProductDto(
                mapping.ProductId,
                mapping.Product.Name,
                mapping.Product.Category,
                mapping.Product.Brand,
                mapping.Product.Price,
                mapping.Product.CurrentStock,
                mapping.Product.ReorderTarget,
                GetStockStatus(mapping.Product),
                mapping.Product.ImageUrl,
                mapping.PartFunction,
                mapping.IsOEM,
                mapping.Notes))
            .ToList());

    private static string GetStockStatus(Product product)
    {
        if (product.CurrentStock == 0) return "OutOfStock";
        return product.ReorderTarget > 0 && product.CurrentStock <= product.ReorderTarget
            ? "LowStock"
            : "InStock";
    }
}
