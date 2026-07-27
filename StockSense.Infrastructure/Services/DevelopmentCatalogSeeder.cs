using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

namespace StockSense.Infrastructure.Services;

public static class DevelopmentCatalogSeeder
{
    public static async Task SeedScooterUpgradeCatalogAsync(ApplicationDbContext context)
    {
        await EnsureBikeAsync(context, "Yamaha", "NMAX V1", 2015, 2019, 155, 15, 14, "Blue Core 155");
        await EnsureBikeAsync(context, "Yamaha", "NMAX V2", 2020, 2023, 155, 15, 14, "Blue Core 155 VVA");
        await EnsureBikeAsync(context, "Yamaha", "NMAX V3", 2024, 2026, 155, 15, 14, "Blue Core 155 VVA");
        await EnsureBikeAsync(context, "Yamaha", "Aerox V1", 2017, 2020, 155, 15, 14, "Blue Core 155 VVA");
        await EnsureBikeAsync(context, "Yamaha", "Aerox V2", 2021, 2024, 155, 15, 14, "Blue Core 155 VVA");
        await EnsureBikeAsync(context, "Yamaha", "Aerox V3", 2025, 2026, 155, 15, 14, "Blue Core 155 VVA");
        await EnsureBikeAsync(context, "Honda", "Click 125", 2020, 2025, 125, 11, 11, "eSP / ACB125");
        await EnsureBikeAsync(context, "Honda", "Click 150", 2018, 2022, 150, 13, 13, "eSP");
        await EnsureBikeAsync(context, "Honda", "Click 160", 2022, 2026, 157, 16, 15, "eSP+");
        await EnsureBikeAsync(context, "Honda", "PCX 160", 2021, 2026, 157, 16, 15, "eSP+");
        await EnsureBikeAsync(context, "Honda", "ADV 160", 2023, 2026, 157, 16, 15, "eSP+");

        await DeactivateDeprecatedBuildSeedsAsync(context);

        await EnsureCategoryAsync(context, "Head", "gauge", 1, false, false, "Cylinder head and valvetrain upgrade choices.");
        await EnsureCategoryAsync(context, "Crank", "rotate-cw", 2, false, false, "Crankshaft and stroker upgrade choices.");
        await EnsureCategoryAsync(context, "Block", "box", 3, false, false, "Cylinder block, bore, and displacement upgrade choices.");
        await EnsureCategoryAsync(context, "Throttle Body", "activity", 4, false, false, "Throttle body and intake airflow upgrade choices.");
        await EnsureCategoryAsync(context, "Pipe", "wind", 5, false, false, "Exhaust pipe choices matched to the engine setup.");
        await EnsureCategoryAsync(context, "ECU", "cpu", 6, false, false, "ECU and tuning controller choices.");
        await context.SaveChangesAsync();

        var bikes = await context.BikeModels.Where(b => b.IsActive).ToListAsync();
        var yamaha155Ids = bikes.Where(b => b.Brand == "Yamaha" && (b.Model.Contains("NMAX") || b.Model.Contains("Aerox"))).Select(b => b.Id).ToList();
        var click125Ids = bikes.Where(b => b.Brand == "Honda" && b.Model == "Click 125").Select(b => b.Id).ToList();
        var honda160Ids = bikes
            .Where(b => b.Brand == "Honda" && (b.Model == "Click 160" || b.Model == "PCX 160" || b.Model == "ADV 160"))
            .Select(b => b.Id)
            .ToList();
        var miniXCompatibleIds = yamaha155Ids.Concat(click125Ids).Concat(honda160Ids).Distinct().ToList();

        var head = await context.UpgradeCategories.FirstAsync(c => c.Name == "Head");
        var crank = await context.UpgradeCategories.FirstAsync(c => c.Name == "Crank");
        var block = await context.UpgradeCategories.FirstAsync(c => c.Name == "Block");
        var throttleBody = await context.UpgradeCategories.FirstAsync(c => c.Name == "Throttle Body");
        var pipe = await context.UpgradeCategories.FirstAsync(c => c.Name == "Pipe");
        var ecu = await context.UpgradeCategories.FirstAsync(c => c.Name == "ECU");

        await EnsureUpgradeProductAsync(context, "JVT 22/25 Big Valve Head", "JVT", "Head", 0, 0, 0, head.Id, yamaha155Ids, 0, 3, 2, -3, 3.0m);
        await EnsureUpgradeProductAsync(context, "JVT 23/26 SuperHead", "JVT", "Head", 0, 0, 0, head.Id, yamaha155Ids, 0, 4, 3, -4, 3.2m);
        await EnsureUpgradeProductAsync(context, "JVT Stock Stroke Hardened Crankshaft", "JVT", "Crank", 0, 0, 0, crank.Id, yamaha155Ids, 0, 1, 1, 1, 3.5m);
        await EnsureUpgradeProductAsync(context, "JVT +4.3mm Stroker Crankshaft", "JVT", "Crank", 0, 0, 0, crank.Id, yamaha155Ids, 17, 2, 3, -5, 4.0m);
        await EnsureUpgradeProductAsync(context, "JVT +9mm Stroker Crankshaft", "JVT", "Crank", 0, 0, 0, crank.Id, yamaha155Ids, 33, 3, 4, -7, 4.5m);
        await EnsureUpgradeProductAsync(context, "JVT 65mm ChromeBore", "JVT", "Block", 0, 0, 0, block.Id, yamaha155Ids, 40, 4, 4, -4, 3.3m);
        await EnsureUpgradeProductAsync(context, "JVT 66mm ChromeBore", "JVT", "Block", 0, 0, 0, block.Id, yamaha155Ids, 44, 5, 5, -7, 3.5m);
        await EnsureUpgradeProductAsync(context, "JVT 34mm Side-Intake Throttle Body", "JVT", "Throttle Body", 0, 0, 0, throttleBody.Id, yamaha155Ids, 0, 2, 1, -1, 1.1m);
        await EnsureUpgradeProductAsync(context, "JVT 36mm Downdraft", "JVT", "Throttle Body", 0, 0, 0, throttleBody.Id, yamaha155Ids, 0, 2, 1, -2, 1.2m);
        await EnsureUpgradeProductAsync(context, "JVT 38mm Downdraft", "JVT", "Throttle Body", 0, 0, 0, throttleBody.Id, yamaha155Ids, 0, 2, 1, -2, 1.2m);
        await EnsureUpgradeProductAsync(context, "JVT V4 32mm Tear Drop Power Pipe", "JVT", "Pipe", 0, 0, 0, pipe.Id, yamaha155Ids, 0, 2, 2, -1, 1.1m);
        await EnsureUpgradeProductAsync(context, "JVT V4 35mm Tear Drop Power Pipe", "JVT", "Pipe", 0, 0, 0, pipe.Id, yamaha155Ids, 0, 2, 2, -1, 1.2m);
        await EnsureUpgradeProductAsync(context, "JVT V4 38mm Tear Drop Power Pipe", "JVT", "Pipe", 0, 0, 0, pipe.Id, yamaha155Ids, 0, 3, 3, -2, 1.3m);

        await EnsureUpgradeProductAsync(context, "JVT Head Click", "JVT", "Head", 15000, 3, 3, head.Id, click125Ids, 0, 3, 2, -4, 3.0m);
        await EnsureUpgradeProductAsync(context, "JVT 22/25 Big Valve Head Click", "JVT", "Head", 0, 0, 0, head.Id, click125Ids, 0, 3, 2, -3, 3.0m);
        await EnsureUpgradeProductAsync(context, "JVT 23/26 SuperHead Click", "JVT", "Head", 0, 0, 0, head.Id, click125Ids, 0, 4, 3, -5, 3.3m);
        await EnsureUpgradeProductAsync(context, "JVT crank +3", "JVT", "Crank", 0, 0, 0, crank.Id, click125Ids, 7, 1, 2, -5, 4.0m);
        await EnsureUpgradeProductAsync(context, "JVT Stock Stroke Hardened Crankshaft Click", "JVT", "Crank", 0, 0, 0, crank.Id, click125Ids, 0, 1, 1, 1, 3.5m);
        await EnsureUpgradeProductAsync(context, "JVT +5.6mm Stroker Crankshaft Click", "JVT", "Crank", 0, 0, 0, crank.Id, click125Ids, 18, 2, 3, -6, 4.3m);
        await EnsureUpgradeProductAsync(context, "JVT +9mm Stroker Crankshaft Click", "JVT", "Crank", 0, 0, 0, crank.Id, click125Ids, 42, 3, 4, -8, 4.8m);
        await EnsureUpgradeProductAsync(context, "JVT 59mm Chrome Bore", "JVT", "Block", 0, 0, 0, block.Id, click125Ids, 33, 4, 4, -7, 3.5m);
        await EnsureUpgradeProductAsync(context, "JVT 60.5mm ChromeBore Click", "JVT", "Block", 0, 0, 0, block.Id, click125Ids, 40, 4, 4, -7, 3.7m);
        await EnsureUpgradeProductAsync(context, "JVT 66mm ChromeBore Click", "JVT", "Block", 0, 0, 0, block.Id, click125Ids, 62, 6, 6, -10, 4.2m);
        await EnsureUpgradeProductAsync(context, "JVT 32mm Side-Intake Throttle Body Click", "JVT", "Throttle Body", 0, 0, 0, throttleBody.Id, click125Ids, 0, 2, 1, -1, 1.1m);
        await EnsureUpgradeProductAsync(context, "JVT 36mm Throttle Body", "JVT", "Throttle Body", 0, 0, 0, throttleBody.Id, click125Ids, 0, 2, 1, -2, 1.2m);
        await EnsureUpgradeProductAsync(context, "JVT 36mm Downdraft Click", "JVT", "Throttle Body", 0, 0, 0, throttleBody.Id, click125Ids, 0, 2, 1, -2, 1.2m);
        await EnsureUpgradeProductAsync(context, "JVT 38mm Downdraft Click", "JVT", "Throttle Body", 0, 0, 0, throttleBody.Id, click125Ids, 0, 3, 2, -3, 1.4m);
        await EnsureUpgradeProductAsync(context, "JVT V3 Pipe", "JVT", "Pipe", 0, 0, 0, pipe.Id, click125Ids, 0, 2, 2, -1, 1.2m);
        await EnsureUpgradeProductAsync(context, "JVT V4 32mm Tear Drop Power Pipe Click", "JVT", "Pipe", 0, 0, 0, pipe.Id, click125Ids, 0, 2, 2, -1, 1.1m);
        await EnsureUpgradeProductAsync(context, "JVT V4 35mm Tear Drop Power Pipe Click", "JVT", "Pipe", 0, 0, 0, pipe.Id, click125Ids, 0, 2, 2, -1, 1.2m);
        await EnsureUpgradeProductAsync(context, "JVT V4 38mm Tear Drop Power Pipe Click", "JVT", "Pipe", 0, 0, 0, pipe.Id, click125Ids, 0, 3, 3, -2, 1.3m);

        await EnsureUpgradeProductAsync(context, "JVT 22/25 Big Valve Head eSP+", "JVT", "Head", 0, 0, 0, head.Id, honda160Ids, 0, 3, 2, -3, 3.0m);
        await EnsureUpgradeProductAsync(context, "JVT 23/26 SuperHead eSP+", "JVT", "Head", 0, 0, 0, head.Id, honda160Ids, 0, 4, 3, -4, 3.2m);
        await EnsureUpgradeProductAsync(context, "JVT Stage 2/3 Camshaft and Valve Springs eSP+", "JVT", "Head", 0, 0, 0, head.Id, honda160Ids, 0, 3, 2, -4, 2.2m);
        await EnsureUpgradeProductAsync(context, "JVT Stock Stroke Hardened Crankshaft eSP+", "JVT", "Crank", 0, 0, 0, crank.Id, honda160Ids, 0, 1, 1, 1, 3.5m);
        await EnsureUpgradeProductAsync(context, "JVT +6mm Stroker Crankshaft eSP+", "JVT", "Crank", 0, 0, 0, crank.Id, honda160Ids, 15, 2, 3, -6, 4.4m);
        await EnsureUpgradeProductAsync(context, "JVT +9mm Stroker Crankshaft eSP+", "JVT", "Crank", 0, 0, 0, crank.Id, honda160Ids, 28, 3, 4, -8, 4.8m);
        await EnsureUpgradeProductAsync(context, "JVT 63mm ChromeBore eSP+", "JVT", "Block", 0, 0, 0, block.Id, honda160Ids, 16, 3, 3, -3, 3.2m);
        await EnsureUpgradeProductAsync(context, "JVT 65mm ChromeBore eSP+", "JVT", "Block", 0, 0, 0, block.Id, honda160Ids, 32, 5, 5, -7, 3.8m);
        await EnsureUpgradeProductAsync(context, "JVT 66mm ChromeBore eSP+", "JVT", "Block", 0, 0, 0, block.Id, honda160Ids, 36, 6, 6, -9, 4.1m);
        await EnsureUpgradeProductAsync(context, "JVT 34mm Side-Intake Throttle Body eSP+", "JVT", "Throttle Body", 0, 0, 0, throttleBody.Id, honda160Ids, 0, 2, 1, -1, 1.1m);
        await EnsureUpgradeProductAsync(context, "JVT 36mm Downdraft eSP+", "JVT", "Throttle Body", 0, 0, 0, throttleBody.Id, honda160Ids, 0, 2, 1, -2, 1.2m);
        await EnsureUpgradeProductAsync(context, "JVT 38mm or 40mm Downdraft eSP+", "JVT", "Throttle Body", 0, 0, 0, throttleBody.Id, honda160Ids, 0, 3, 2, -3, 1.4m);
        await EnsureUpgradeProductAsync(context, "JVT 160cc-180cc Fuel Injector eSP+", "JVT", "Throttle Body", 0, 0, 0, throttleBody.Id, honda160Ids, 0, 2, 1, -1, 0.8m);
        await EnsureUpgradeProductAsync(context, "JVT Dual-Injector Intake Manifold eSP+", "JVT", "Throttle Body", 0, 0, 0, throttleBody.Id, honda160Ids, 0, 2, 1, -2, 1.1m);
        await EnsureUpgradeProductAsync(context, "JVT V4 32mm Tear Drop Power Pipe eSP+", "JVT", "Pipe", 0, 0, 0, pipe.Id, honda160Ids, 0, 2, 2, -1, 1.1m);
        await EnsureUpgradeProductAsync(context, "JVT V4 35mm Tear Drop Power Pipe eSP+", "JVT", "Pipe", 0, 0, 0, pipe.Id, honda160Ids, 0, 2, 2, -1, 1.2m);
        await EnsureUpgradeProductAsync(context, "JVT V4 38mm Tear Drop Power Pipe eSP+", "JVT", "Pipe", 0, 0, 0, pipe.Id, honda160Ids, 0, 3, 3, -2, 1.3m);
        await EnsureUpgradeProductAsync(context, "JVT E-Power Pipe 32mm Big Elbow eSP+", "JVT", "Pipe", 0, 0, 0, pipe.Id, honda160Ids, 0, 2, 2, -1, 1.2m);

        await EnsureUpgradeProductAsync(context, "aRacer Mini X Lite", "aRacer", "ECU", 0, 0, 0, ecu.Id, miniXCompatibleIds, 0, 3, 2, -2, 1.4m);
        await EnsureUpgradeProductAsync(context, "Aracer RC Mini X", "aRacer", "ECU", 27758.24m, 0, 0, ecu.Id, honda160Ids, 0, 4, 3, -2, 1.6m);

        await EnsureSalesHistoryAsync(context);
        await context.SaveChangesAsync();
    }

    private static async Task EnsureBikeAsync(ApplicationDbContext context, string brand, string model, int start, int end, int cc, int hp, int torque, string engine)
    {
        var bike = await context.BikeModels.FirstOrDefaultAsync(b => b.Brand == brand && b.Model == model);
        if (bike == null)
        {
            context.BikeModels.Add(new BikeModel
            {
                Brand = brand,
                Model = model,
                YearStart = start,
                YearEnd = end,
                BaseCC = cc,
                BaseHP = hp,
                BaseTorque = torque,
                EngineCode = engine,
                Notes = "Development seed for StockSense build compatibility.",
                IsActive = true,
            });
            return;
        }

        bike.YearStart = start;
        bike.YearEnd = end;
        bike.BaseCC = cc;
        bike.BaseHP = hp;
        bike.BaseTorque = torque;
        bike.EngineCode = engine;
        bike.Notes = "Development seed for StockSense build compatibility.";
        bike.IsActive = true;
    }

    private static async Task EnsureCategoryAsync(ApplicationDbContext context, string name, string icon, int order, bool required, bool multiple, string description)
    {
        var category = await context.UpgradeCategories.FirstOrDefaultAsync(c => c.Name == name);
        if (category == null)
        {
            context.UpgradeCategories.Add(new UpgradeCategory
            {
                Name = name,
                Icon = icon,
                DisplayOrder = order,
                IsRequired = required,
                AllowsMultiple = multiple,
                Description = description,
                CompatibilityNotes = "Verify physical fitment and tuning requirements before installation.",
                IsActive = true,
            });
            return;
        }

        category.Icon = icon;
        category.DisplayOrder = order;
        category.IsRequired = required;
        category.AllowsMultiple = multiple;
        category.Description = description;
        category.IsActive = true;
    }

    private static async Task DeactivateDeprecatedBuildSeedsAsync(ApplicationDbContext context)
    {
        var deprecatedCategories = new[]
        {
            "Engine / Big Bore",
            "ECU / Fuel",
            "CVT / Transmission",
            "Exhaust",
            "Brakes / Handling",
        };

        var categories = await context.UpgradeCategories
            .Where(c => deprecatedCategories.Contains(c.Name))
            .ToListAsync();
        foreach (var category in categories)
            category.IsActive = false;

        var deprecatedProducts = new[]
        {
            "Honda Click/PCX/ADV Performance Pipe",
            "NMAX/Aerox Full System Exhaust",
            "RCB Brake Pad Set Scooter",
            "Pirelli Angel Scooter Tire 14",
            "JVT V3 Pulley Set NMAX/Aerox",
            "Honda Click Racing Pulley Set",
            "Dr Pulley Slider Weight Set",
            "1500 RPM Clutch Spring Set",
            "Yamaha 155 232cc Big Bore Kit",
            "Yamaha 155 Stroker Crank Kit",
            "Honda Click 164cc Bore Kit",
            "Performance Camshaft Scooter",
            "Fuel Injector 180cc Scooter",
            "ECU Fuel Controller Scooter",
            "JVT +9 CrankShaft",
            "JVT V4 38mm Tear Drop",
            "Aracer MiniXLite",
            "Yamaha 155 Performance ECU",
            "Yamaha 155 Racing Exhaust",
            "JVT V3 Pulley Set (NMAX/Aerox)",
            "JVT Clutch Spring",
            "JVT Fly Ball (NMAX)",
        };

        var parts = await context.UpgradeParts
            .Include(p => p.Product)
            .Where(p => p.Product != null && deprecatedProducts.Contains(p.Product.Name))
            .ToListAsync();
        foreach (var part in parts)
            part.IsActive = false;
    }

    private static async Task EnsureUpgradeProductAsync(
        ApplicationDbContext context,
        string name,
        string brand,
        string category,
        decimal price,
        int stock,
        int reorder,
        int upgradeCategoryId,
        List<int> compatibleModelIds,
        int ccGain,
        int hpGain,
        int torqueGain,
        int reliabilityImpact,
        decimal laborHours)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Name == name);
        if (product == null)
        {
            product = new Product
            {
                Name = name,
                Brand = brand,
                Category = category,
                Price = price,
                CurrentStock = stock,
                ReorderTarget = reorder,
                ImageUrl = $"https://placehold.co/600x400/111827/ffffff?text={Uri.EscapeDataString(name)}",
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();
        }
        else
        {
            product.Brand = brand;
            product.Category = category;
            product.Price = price;
            product.CurrentStock = stock;
            product.ReorderTarget = reorder;
            if (string.IsNullOrWhiteSpace(product.ImageUrl) || product.ImageUrl.Contains("300x200"))
                product.ImageUrl = $"https://placehold.co/600x400/111827/ffffff?text={Uri.EscapeDataString(name)}";
        }

        var part = await context.UpgradeParts.FirstOrDefaultAsync(p => p.ProductId == product.Id);
        if (part == null)
        {
            context.UpgradeParts.Add(new UpgradePart
            {
                ProductId = product.Id,
                UpgradeCategoryId = upgradeCategoryId,
                CCGain = ccGain,
                HPGain = hpGain,
                TorqueGain = torqueGain,
                ReliabilityImpact = reliabilityImpact,
                ListPrice = price,
                EstimatedLaborHours = laborHours,
                CompatibleModelsJson = JsonSerializer.Serialize(compatibleModelIds),
                InstallNotes = "Development catalog seed. Mechanic must confirm fitment before installation.",
                IsActive = true,
            });
            return;
        }

        part.UpgradeCategoryId = upgradeCategoryId;
        part.CCGain = ccGain;
        part.HPGain = hpGain;
        part.TorqueGain = torqueGain;
        part.ReliabilityImpact = reliabilityImpact;
        part.ListPrice = price;
        part.EstimatedLaborHours = laborHours;
        part.CompatibleModelsJson = JsonSerializer.Serialize(compatibleModelIds);
        part.IsActive = true;
    }

    private static async Task EnsureSalesHistoryAsync(ApplicationDbContext context)
    {
        if (await context.SalesHistory.AnyAsync()) return;

        var products = await context.Products.Take(12).ToListAsync();
        var now = DateTime.Today;
        var id = 1;
        foreach (var product in products)
        {
            var qty = product.Category.Contains("Oil", StringComparison.OrdinalIgnoreCase) ? 24 :
                product.Category.Contains("CVT", StringComparison.OrdinalIgnoreCase) ? 14 :
                product.Category.Contains("Tire", StringComparison.OrdinalIgnoreCase) ? 8 :
                product.Category.Contains("Exhaust", StringComparison.OrdinalIgnoreCase) ? 6 : 5;

            context.SalesHistory.Add(new SalesHistory
            {
                Date = now.AddDays(-id).ToString("yyyy-MM-dd"),
                ProductID = product.Id.ToString(),
                ProductName = product.Name,
                Brand = product.Brand,
                Category = product.Category,
                QtySold = qty,
                UnitPrice = (float)product.Price,
                TotalSales = (float)(product.Price * qty),
                MonthNum = now.Month,
                Year = now.Year,
            });
            id++;
        }
    }
}
