using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StockSense.Infrastructure.Data;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Services;

public class KnowledgeBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KnowledgeBase> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private ConcurrentDictionary<string, object> _cache = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    public KnowledgeBase(IServiceScopeFactory scopeFactory, ILogger<KnowledgeBase> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<List<Product>> GetProducts()
    {
        await RefreshIfStale();
        return _cache.GetOrAdd("products", _ => new List<Product>()) as List<Product> ?? new();
    }

    public async Task<List<StoreService>> GetServices()
    {
        await RefreshIfStale();
        return _cache.GetOrAdd("services", _ => new List<StoreService>()) as List<StoreService> ?? new();
    }

    public async Task<List<PreBuiltPackage>> GetPreBuilds()
    {
        await RefreshIfStale();
        return _cache.GetOrAdd("prebuilds", _ => new List<PreBuiltPackage>()) as List<PreBuiltPackage> ?? new();
    }

    public async Task<List<UpgradePart>> GetUpgradeParts()
    {
        await RefreshIfStale();
        return _cache.GetOrAdd("upgradeParts", _ => new List<UpgradePart>()) as List<UpgradePart> ?? new();
    }

    public async Task<List<BikeModel>> GetBikeModels()
    {
        await RefreshIfStale();
        return _cache.GetOrAdd("bikeModels", _ => new List<BikeModel>()) as List<BikeModel> ?? new();
    }

    public async Task<List<Mechanic>> GetMechanics()
    {
        await RefreshIfStale();
        return _cache.GetOrAdd("mechanics", _ => new List<Mechanic>()) as List<Mechanic> ?? new();
    }

    public async Task<List<Appointment>> GetAppointments()
    {
        await RefreshIfStale();
        return _cache.GetOrAdd("appointments", _ => new List<Appointment>()) as List<Appointment> ?? new();
    }

    public async Task<List<SalesHistory>> GetSalesHistory()
    {
        await RefreshIfStale();
        return _cache.GetOrAdd("salesHistory", _ => new List<SalesHistory>()) as List<SalesHistory> ?? new();
    }

    public async Task<List<RagDocument>> GetRetrievalDocuments()
    {
        var products = await GetProducts();
        var services = await GetServices();
        var prebuilds = await GetPreBuilds();
        var upgradeParts = await GetUpgradeParts();
        var bikeModels = await GetBikeModels();
        var mechanics = await GetMechanics();
        var appointments = await GetAppointments();
        var documents = products.Select(product => new RagDocument
        {
            Id = $"product:{product.Id}",
            Type = "Product",
            Title = product.Name,
            Text = $"{product.Name}. Brand {product.Brand}. Category {product.Category}. " +
                   $"Price {product.Price:N2} pesos. Current stock {product.CurrentStock}. Motorcycle performance part.",
            Link = "/build",
            Price = product.Price,
            CurrentStock = product.CurrentStock,
        }).ToList();

        documents.AddRange(services.Where(service => string.Equals(service.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .Select(service => new RagDocument
            {
                Id = $"service:{service.Id}",
                Type = "Service",
                Title = service.Name,
                Text = $"{service.Name}. Category {service.Category}. Service price {service.Price:N2} pesos. " +
                       $"Estimated duration {service.EstimatedMinutes} minutes. Required products " +
                       string.Join(' ', service.RequiredProducts.Select(product => product.Name)),
                Link = "/appointment",
                Price = service.Price,
                DurationMinutes = service.EstimatedMinutes,
            }));

        documents.AddRange(prebuilds.Select(package => new RagDocument
        {
            Id = $"build:{package.Id}",
            Type = "Build",
            Title = package.Name,
            Text = $"{package.Name}. {package.Description}. Compatible with {package.CompatibleBrand} " +
                   $"{package.CompatibleModel}. Target {package.TargetCC}. Adds approximately {package.EstimatedAddedCC} cc. " +
                   $"Included parts {string.Join(' ', package.IncludedProducts.Select(product => product.Name))}.",
            Link = "/build",
            Price = package.TotalPrice,
        }));

        documents.AddRange(upgradeParts.Select(part =>
        {
            var compatibleModels = FormatCompatibleModels(part.CompatibleModelsJson, bikeModels);
            return new RagDocument
        {
            Id = $"upgrade-part:{part.Id}",
            Type = "UpgradePart",
            Title = part.Product?.Name ?? $"Upgrade part {part.Id}",
            Text = $"{part.Product?.Name}. Brand {part.Product?.Brand}. Category {part.Category?.Name}. " +
                   $"Compatible models {compatibleModels}. Gains {part.CCGain} cc, {part.HPGain} hp, {part.TorqueGain} Nm torque. " +
                   $"Reliability impact {part.ReliabilityImpact}. Stock {part.Product?.CurrentStock ?? 0}. Install notes {part.InstallNotes}.",
            Link = "/build",
            Price = part.ListPrice,
            CurrentStock = part.Product?.CurrentStock,
        };
        }));

        documents.AddRange(mechanics.Where(mechanic => mechanic.IsActive).Select(mechanic => new RagDocument
        {
            Id = $"mechanic:{mechanic.Id}",
            Type = "Mechanic",
            Title = mechanic.Name,
            Text = $"{mechanic.Name}. Active StockSense mechanic available for appointment assignment.",
            Link = "/appointment",
        }));

        documents.AddRange(appointments
            .Where(appointment => appointment.AppointmentDate >= DateTime.Today.AddDays(-1))
            .OrderBy(appointment => appointment.AppointmentDate)
            .ThenBy(appointment => appointment.TimeSlot)
            .Take(30)
            .Select(appointment => new RagDocument
            {
                Id = $"appointment:{appointment.Id}",
                Type = "Appointment",
                Title = $"{appointment.MechanicName} {appointment.AppointmentDate:MMM d} {appointment.TimeSlot}",
                Text = $"{appointment.Status} appointment for {appointment.ServicesRequested}. Mechanic {appointment.MechanicName}. " +
                       $"Date {appointment.AppointmentDate:MMMM d, yyyy}. Time {appointment.TimeSlot}. Duration {appointment.DurationMinutes} minutes.",
                Link = "/appointment",
                Price = appointment.TotalAmount,
                DurationMinutes = appointment.DurationMinutes,
            }));
        return documents;
    }

    public async Task<string> GetBuildGuidanceText(string message)
    {
        var normalized = message.ToLowerInvariant();
        var bikeModels = await GetBikeModels();
        var upgradeParts = await GetUpgradeParts();

        if (Regex.IsMatch(normalized, @"\b(acceptable|supported|available)\b.*\b(motorcycle|motorcycles|bike|bikes|model|models)\b") ||
            Regex.IsMatch(normalized, @"\b(motorcycle|motorcycles|bike|bikes|model|models)\b"))
        {
            return FormatBikeModels(bikeModels);
        }

        var bike = FindBikeModel(normalized, bikeModels);
        var targetCc = ExtractTargetCc(normalized);

        if (bike != null && targetCc > bike.BaseCC)
        {
            var neededCc = targetCc - bike.BaseCC;
            var ccParts = upgradeParts
                .Where(part => part.Product != null && IsCompatibleWithBike(part, bike) && part.CCGain > 0)
                .OrderByDescending(part => part.CCGain)
                .ThenBy(part => part.ListPrice)
                .ToList();

            var selectedParts = SelectTargetCcParts(ccParts, neededCc);
            var projectedGain = selectedParts.Sum(part => part.CCGain);

            if (selectedParts.Any())
            {
                var finalCc = bike.BaseCC + projectedGain;
                var lines = selectedParts.Select(part =>
                    $"- {part.Product.Name}: +{part.CCGain} cc, +{part.HPGain} hp, +{part.TorqueGain} Nm torque, stock {part.Product.CurrentStock}, build price PHP {part.ListPrice:N0}.");
                var remaining = finalCc >= targetCc
                    ? $"This reaches an estimated {finalCc} cc from the stock {bike.BaseCC} cc base."
                    : $"This only reaches an estimated {finalCc} cc from the stock {bike.BaseCC} cc base, so more build data is needed to verify a full {targetCc} cc setup.";

                return $"Based on StockSense build records for {bike.DisplayName}, a {targetCc} cc target needs about +{neededCc} cc:\n" +
                       string.Join("\n", lines) +
                       $"\n{remaining} Use the build wizard to verify compatibility, tuning, labor, and reliability before installation." +
                       BuildMechanicGuidanceWarning(selectedParts);
            }
        }

        var productTerms = ExtractUsefulTerms(normalized);
        var isPipeQuestion = Regex.IsMatch(normalized, @"\b(pipe|pipes|muffler|exhaust|tambutso)\b");
        var matchingParts = upgradeParts
            .Where(part => part.Product != null)
            .Where(part => bike == null || IsCompatibleWithBike(part, bike))
            .Where(part => !isPipeQuestion || IsExhaustPart(part))
            .Where(part => !productTerms.Any() || productTerms.Any(term =>
                part.Product.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                part.Product.Brand.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (part.Category?.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)))
            .Take(5)
            .ToList();

        if (matchingParts.Any())
        {
            var bikeText = bike == null ? "the selected motorcycle" : bike.DisplayName;
            var lines = matchingParts.Select(part =>
                $"- {part.Product.Name}: compatible with {bikeText}; +{part.CCGain} cc, +{part.HPGain} hp, +{part.TorqueGain} Nm torque; stock {part.Product.CurrentStock}; build price PHP {part.ListPrice:N0}.");
            return "Based on StockSense build records:\n" + string.Join("\n", lines) + "\nStart from the build wizard to verify the complete tuning projection." +
                   BuildMechanicGuidanceWarning(matchingParts);
        }

        if (bike != null)
        {
            var compatibleParts = upgradeParts
                .Where(part => part.Product != null && IsCompatibleWithBike(part, bike))
                .Where(part => !isPipeQuestion || IsExhaustPart(part))
                .Take(5)
                .ToList();

            if (compatibleParts.Any())
            {
                var lines = compatibleParts.Select(part =>
                    $"- {part.Product.Name}: {part.Category?.Name ?? part.Product.Category}, stock {part.Product.CurrentStock}, build price PHP {part.ListPrice:N0}.");
                return $"I could not find an exact product/category match for that question, but these StockSense upgrade parts are compatible with {bike.DisplayName}:\n" +
                   string.Join("\n", lines) +
                   BuildMechanicGuidanceWarning(compatibleParts);
            }

            if (isPipeQuestion)
                return $"I do not see an active pipe/exhaust product for {bike.DisplayName} in the StockSense build catalog yet. Add a Honda Click/PCX/ADV exhaust product and link it as an Exhaust upgrade part so I can verify compatibility and stock.";
        }

        var packages = await GetPreBuilds();
        var packageMatches = packages
            .Where(package => package.IsActive)
            .Where(package => bike == null ||
                string.Equals(package.CompatibleBrand, bike.Brand, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(package.CompatibleModel, bike.Model, StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();

        if (packageMatches.Any())
        {
            var lines = packageMatches.Select(package =>
                $"- {package.Name}: {package.CompatibleBrand} {package.CompatibleModel}, target {package.TargetCC}, estimated +{package.EstimatedAddedCC} cc, PHP {package.TotalPrice:N0}.");
            return "These prebuilt packages match the motorcycle/build question:\n" + string.Join("\n", lines) + "\nOpen the build wizard to choose a package or switch to custom build.";
        }

        var categoryMatches = FindUpgradePartsByCategory(normalized, upgradeParts).Take(5).ToList();
        if (categoryMatches.Any())
        {
            var lines = categoryMatches.Select(part =>
                $"- {part.Product.Name}: {part.Category?.Name ?? part.Product.Category}, stock {part.Product.CurrentStock}, build price PHP {part.ListPrice:N0}.");
            return "I found these StockSense upgrade parts in that category:\n" + string.Join("\n", lines) +
                   "\nFor compatibility, include the motorcycle model exactly as it appears in the build catalog.";
        }

        return "I could not verify that from the current StockSense build records. Ask for supported motorcycle models, or include the exact product/category and motorcycle model.";
    }

    public async Task<string> SearchPartsText(string message)
    {
        var parts = await GetProducts();
        message = message.ToLowerInvariant();

        if (Regex.IsMatch(message, @"\b(low|critical|reorder|restock|stocking|need\s+stock|what\s+to\s+stock)\b"))
        {
            var restock = parts
                .Where(product => product.CurrentStock <= product.ReorderTarget)
                .OrderBy(product => product.CurrentStock - product.ReorderTarget)
                .ThenBy(product => product.Name)
                .Take(8)
                .ToList();

            if (!restock.Any())
                return "No products are currently at or below their reorder target in the StockSense inventory records.";

            var lines = restock.Select(product =>
                $"- {product.Name}: stock {product.CurrentStock}, reorder target {product.ReorderTarget}, suggested restock at least {Math.Max(product.ReorderTarget - product.CurrentStock, 1)}.");
            return "Inventory items that need attention:\n" + string.Join("\n", lines);
        }

        if (Regex.IsMatch(message, @"\b(pipe|pipes|muffler|exhaust|tambutso)\b"))
        {
            var exhaustProducts = parts
                .Where(product =>
                    product.Name.Contains("exhaust", StringComparison.OrdinalIgnoreCase) ||
                    product.Name.Contains("pipe", StringComparison.OrdinalIgnoreCase) ||
                    product.Name.Contains("muffler", StringComparison.OrdinalIgnoreCase) ||
                    product.Category.Contains("exhaust", StringComparison.OrdinalIgnoreCase))
                .Take(8)
                .ToList();

            if (!exhaustProducts.Any())
                return "I do not see any active pipe/exhaust products in StockSense inventory yet. Add exhaust products to inventory and link them to the build catalog for compatibility answers.";

            var exhaustLines = exhaustProducts.Select(product =>
                $"- {product.Name}: PHP {product.Price:N0}, stock {product.CurrentStock}, reorder target {product.ReorderTarget}, category {product.Category}.");
            return "Available StockSense pipe/exhaust inventory:\n" + string.Join("\n", exhaustLines);
        }

        if (Regex.IsMatch(message, @"\b(oil|oils|langis)\b") &&
            Regex.IsMatch(message, @"\b(use|recommend|recommended|available|what|which|ano|pwede|puwede|for|sa|change\s+oil)\b"))
        {
            var oilProducts = parts
                .Where(IsEngineOilProduct)
                .Where(product => IsOilCompatibleWithModel(product, message))
                .OrderBy(product => product.Brand)
                .ThenBy(product => product.Price)
                .Take(8)
                .ToList();

            if (!oilProducts.Any())
                return "No engine-oil products are currently configured in StockSense inventory. Add oils like 10W-40 or scooter engine oil so I can recommend available options.";

            var modelHint = ExtractModelHint(message);
            var oilLines = oilProducts.Select(product =>
                $"- {product.Name}: PHP {product.Price:N0}, stock {product.CurrentStock}, brand {product.Brand}, category {product.Category}.");

            return $"Available StockSense engine-oil products{modelHint}:\n" +
                   string.Join("\n", oilLines) +
                   $"\nFor {GetOilModelText(message)} daily use, confirm the viscosity against the owner's manual and mechanic recommendation before purchase.";
        }

        if (Regex.IsMatch(message, @"\b(products?|items?|inventory|catalog)\b") &&
            Regex.IsMatch(message, @"\b(available|show|list|what|ano|meron)\b"))
        {
            if (!parts.Any())
                return "No product records are currently configured in StockSense inventory. Add or seed products first so I can show available stock and prices.";

            var available = parts
                .Where(product => product.CurrentStock > 0)
                .OrderBy(product => product.Category)
                .ThenBy(product => product.Name)
                .Take(10)
                .ToList();

            var availableLines = available.Select(product =>
                $"- {product.Name}: PHP {product.Price:N0}, stock {product.CurrentStock}, category {product.Category}.");
            return "Available StockSense inventory products:\n" + string.Join("\n", availableLines);
        }

        var keywords = message.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeSearchToken)
            .Where(w => w.Length > 2)
            .Where(w => !IsGenericInventoryWord(w))
            .ToList();

        if (!keywords.Any() && !parts.Any())
            return "No product records are currently configured in StockSense inventory. Add or seed products first so I can show available stock and prices.";

        var matches = parts.Where(p =>
            !keywords.Any() || keywords.Any(k =>
                p.Name.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                p.Brand.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                (p.Category?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false)))
            .Take(5)
            .ToList();

        if (!matches.Any())
        {
            var categoryMatches = FindProductsByCategory(message, parts).Take(8).ToList();
            if (categoryMatches.Any())
            {
                var categoryLines = categoryMatches.Select(product =>
                    $"- {product.Name}: PHP {product.Price:N0}, stock {product.CurrentStock}, reorder target {product.ReorderTarget}, category {product.Category}.");
                return "I found these StockSense inventory records in that category:\n" + string.Join("\n", categoryLines);
            }

            return "I could not find a matching StockSense inventory item. Try the exact product name, brand, category, or barcode. If this product should exist, add or seed it in inventory first.";
        }

        var productLines = matches.Select(product =>
            $"- {product.Name}: PHP {product.Price:N0}, stock {product.CurrentStock}, reorder target {product.ReorderTarget}, category {product.Category}.");
        return "Matching StockSense inventory records:\n" + string.Join("\n", productLines);
    }

    public async Task<string> GetInventoryInsightsText(string message)
    {
        var products = await GetProducts();
        var sales = await GetSalesHistory();
        var normalized = message.ToLowerInvariant();

        if (Regex.IsMatch(normalized, @"\b(best\s*selling|top\s*selling|fast\s*moving|best\s*orders?)\b"))
        {
            if (sales.Any())
            {
                var lines = sales
                    .GroupBy(sale => string.IsNullOrWhiteSpace(sale.ProductName) ? sale.ProductID : sale.ProductName)
                    .Select(group => new
                    {
                        ProductName = group.Key,
                        Qty = group.Sum(sale => sale.QtySold),
                        Sales = group.Sum(sale => sale.TotalSales),
                    })
                    .OrderByDescending(item => item.Qty)
                    .ThenByDescending(item => item.Sales)
                    .Take(8)
                    .Select(item => $"- {item.ProductName}: {item.Qty:N0} sold, PHP {item.Sales:N0} sales.");

                return "Best-selling StockSense products based on sales history:\n" + string.Join("\n", lines);
            }

            var fallbackLines = products
                .OrderByDescending(product => product.ReorderTarget)
                .ThenBy(product => product.CurrentStock)
                .Take(8)
                .Select(product => $"- {product.Name}: stock {product.CurrentStock}, reorder target {product.ReorderTarget}, category {product.Category}.");
            return "No sales history is recorded yet, so this is an estimated priority list based on reorder targets and current stock:\n" + string.Join("\n", fallbackLines);
        }

        var restock = products
            .Select(product =>
            {
                var productSales = sales.Where(sale =>
                    sale.ProductID == product.Id.ToString() ||
                    sale.ProductName.Equals(product.Name, StringComparison.OrdinalIgnoreCase));
                var monthlyDemand = productSales.Any() ? Math.Ceiling(productSales.Sum(sale => sale.QtySold) / Math.Max(productSales.Select(sale => $"{sale.Year}-{sale.MonthNum}").Distinct().Count(), 1)) : 0;
                var suggested = Math.Max(product.ReorderTarget - product.CurrentStock, 0);
                suggested = Math.Max(suggested, (int)Math.Max(0, monthlyDemand - product.CurrentStock));
                return new { Product = product, MonthlyDemand = monthlyDemand, Suggested = suggested };
            })
            .Where(item => item.Suggested > 0 || item.Product.CurrentStock <= item.Product.ReorderTarget)
            .OrderByDescending(item => item.Suggested)
            .ThenBy(item => item.Product.CurrentStock - item.Product.ReorderTarget)
            .Take(8)
            .ToList();

        if (!restock.Any())
            return "No urgent reorder is detected from current stock and sales history. For next month, monitor fast-moving oils, CVT consumables, brake pads, tires, and popular exhaust/CVT upgrade parts.";

        var orderLines = restock.Select(item =>
            $"- {item.Product.Name}: stock {item.Product.CurrentStock}, reorder target {item.Product.ReorderTarget}, estimated monthly demand {item.MonthlyDemand:N0}, suggested order {Math.Max(item.Suggested, 1)}.");
        return "Recommended products to order next based on stock and sales history:\n" + string.Join("\n", orderLines);
    }

    public Task<string> GetMaintenanceIntervalText(string message)
    {
        var normalized = message.ToLowerInvariant();
        var model = "that scooter";
        if (normalized.Contains("click")) model = "Honda Click 125";
        else if (normalized.Contains("pcx")) model = "Honda PCX";
        else if (normalized.Contains("adv")) model = "Honda ADV";
        else if (normalized.Contains("nmax")) model = "Yamaha NMAX";
        else if (normalized.Contains("aerox")) model = "Yamaha Aerox";

        return Task.FromResult(
            $"For a mostly stock {model}, a safe StockSense maintenance guide is: change engine oil every 2,000-3,000 km for daily city use, or earlier around 1,500-2,000 km if traffic is heavy, riding is aggressive, or the engine is modified. Use the oil grade recommended by the motorcycle manual, and book PMS if the bike has noise, overheating, hard starting, oil leaks, or power loss. For exact warranty-safe intervals, follow the owner's manual and let the mechanic confirm the bike condition.");
    }

    public async Task<string> GetAppointmentAvailabilityText()
    {
        var mechanics = (await GetMechanics()).Where(mechanic => mechanic.IsActive).OrderBy(mechanic => mechanic.Name).ToList();
        var appointments = (await GetAppointments())
            .Where(appointment => appointment.AppointmentDate >= DateTime.Today &&
                                  !string.Equals(appointment.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            .OrderBy(appointment => appointment.AppointmentDate)
            .ThenBy(appointment => appointment.TimeSlot)
            .Take(8)
            .ToList();

        var mechanicText = mechanics.Any()
            ? "Active mechanics: " + string.Join(", ", mechanics.Select(mechanic => mechanic.Name)) + "."
            : "The appointment module is available, but no active mechanic records are configured yet.";
        var appointmentText = appointments.Any()
            ? "\nUpcoming booked slots:\n" + string.Join("\n", appointments.Select(appointment =>
                $"- {appointment.AppointmentDate:MMM d} {appointment.TimeSlot}: {appointment.MechanicName}, {appointment.ServicesRequested}, {appointment.Status}."))
            : "\nNo upcoming appointment records are currently listed.";

        return mechanicText + appointmentText + "\nAdd active mechanics and appointment slots to make live booking availability visible.";
    }
    public async Task<string> SearchServiceInfo(string message)
    {
        var services = await GetServices();
        var products = await GetProducts();
        message = message.ToLowerInvariant();

        if (Regex.IsMatch(message, @"\b(every\s+what\s+km|how\s+many\s+km|when\s+should|change\s+oil\s+interval|oil\s+change\s+interval|ilang\s+km)\b"))
            return await GetMaintenanceIntervalText(message);

        if (message.Contains("oil") || message.Contains("oils") || message.Contains("langis"))
        {
            var oilProducts = FindProductsByCategory("oil", products).Take(8).ToList();
            if (oilProducts.Any())
            {
                var productLines = oilProducts.Select(product =>
                    $"- {product.Name}: PHP {product.Price:N0}, stock {product.CurrentStock}, reorder target {product.ReorderTarget}.");
                return "Available oil-related StockSense inventory:\n" + string.Join("\n", productLines);
            }
        }

        if (!services.Any())
            return "No active service records are currently configured in StockSense. Add services like PMS, change oil, CVT cleaning, or general check-up with prices so I can answer service-price questions.";

        bool isOil = new[] { "oil", "change oil", "oil change", "palit langis" }.Any(k => message.Contains(k));
        bool isPms = new[] { "pms", "tune up", "tune-up", "check up", "general check" }.Any(k => message.Contains(k));
        bool wantsServiceList = new[] { "service", "services", "price", "prices", "available" }.Any(k => message.Contains(k));

        if (isOil)
        {
            var oilService = services.FirstOrDefault(service => service.Name.Contains("Oil", StringComparison.OrdinalIgnoreCase));
            if (oilService != null)
                return FormatService(oilService);
        }

        if (isPms)
        {
            var pmsService = services.FirstOrDefault(service =>
                service.Name.Contains("PMS", StringComparison.OrdinalIgnoreCase) ||
                service.Name.Contains("Tune", StringComparison.OrdinalIgnoreCase) ||
                service.Name.Contains("Check", StringComparison.OrdinalIgnoreCase));

            if (pmsService != null)
                return FormatService(pmsService);

            return "I do not see a specific PMS service record yet. Current configured services are:\n" + FormatServices(services);
        }

        if (wantsServiceList)
            return "Current configured StockSense services:\n" + FormatServices(services);

        return "Current configured StockSense services:\n" + FormatServices(services);
    }
    private async Task RefreshIfStale()
    {
        if (DateTime.UtcNow - _lastRefresh < CacheDuration)
            return;

        await _refreshLock.WaitAsync();
        try
        {
            if (DateTime.UtcNow - _lastRefresh < CacheDuration)
                return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var products = await db.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            var services = await db.StoreServices
                .AsNoTracking()
                .Include(s => s.RequiredProducts)
                .ToListAsync();

            var prebuilds = await db.PreBuiltPackages
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Include(p => p.IncludedProducts)
                .ToListAsync();

            var upgradeParts = await db.UpgradeParts
                .AsNoTracking()
                .Include(part => part.Product)
                .Include(part => part.Category)
                .Where(part => part.IsActive &&
                               part.Product != null &&
                               part.Category != null &&
                               part.Category.IsActive)
                .ToListAsync();

            var bikeModels = await db.BikeModels
                .AsNoTracking()
                .Where(model => model.IsActive)
                .ToListAsync();

            var mechanics = await db.Mechanics
                .AsNoTracking()
                .ToListAsync();

            var appointments = await db.Appointments
                .AsNoTracking()
                .Where(appointment => appointment.AppointmentDate >= DateTime.Today.AddDays(-1))
                .ToListAsync();

            var salesHistory = await db.SalesHistory
                .AsNoTracking()
                .ToListAsync();

            _cache = new ConcurrentDictionary<string, object>(new Dictionary<string, object>
            {
                ["products"] = products,
                ["services"] = services,
                ["prebuilds"] = prebuilds,
                ["upgradeParts"] = upgradeParts,
                ["bikeModels"] = bikeModels,
                ["mechanics"] = mechanics,
                ["appointments"] = appointments,
                ["salesHistory"] = salesHistory,
            });
            _lastRefresh = DateTime.UtcNow;
            SafeLogInformation(
                "Catalog retrieval cache refreshed: {ProductCount} products, {ServiceCount} services, {BuildCount} pre-builds.",
                products.Count, services.Count, prebuilds.Count);
        }
        catch (Exception exception)
        {
            SafeLogError(exception, "Failed to refresh the live catalog retrieval cache.");
            _cache.TryAdd("products", new List<Product>());
            _cache.TryAdd("services", new List<StoreService>());
            _cache.TryAdd("prebuilds", new List<PreBuiltPackage>());
            _cache.TryAdd("upgradeParts", new List<UpgradePart>());
            _cache.TryAdd("bikeModels", new List<BikeModel>());
            _cache.TryAdd("mechanics", new List<Mechanic>());
            _cache.TryAdd("appointments", new List<Appointment>());
            _cache.TryAdd("salesHistory", new List<SalesHistory>());
            _lastRefresh = DateTime.UtcNow;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static List<string> ExtractUsefulTerms(string message)
    {
        var ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "will", "this", "that", "work", "fit", "compatible", "gain", "gains", "motor", "motorcycle",
            "bike", "setup", "stock", "price", "what", "with", "for", "the", "and", "can", "get", "from",
            "available", "availability", "acceptable", "supported", "your", "are", "currently", "right", "now",
            "pipe", "pipes", "muffler", "tambutso"
        };

        return Regex.Matches(message, @"[a-z0-9]+(?:-[a-z0-9]+)?", RegexOptions.IgnoreCase)
            .Select(match => match.Value)
            .Select(NormalizeSearchToken)
            .Where(term => term.Length > 2 && !ignored.Contains(term))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void SafeLogInformation(string message, params object[] args)
    {
        try
        {
            _logger.LogInformation(message, args);
        }
        catch
        {
            Console.WriteLine(message);
        }
    }

    private void SafeLogError(Exception exception, string message)
    {
        try
        {
            _logger.LogError(exception, message);
        }
        catch
        {
            Console.WriteLine($"{message} {exception.Message}");
        }
    }

    private static string FormatBikeModels(List<BikeModel> bikeModels)
    {
        if (!bikeModels.Any())
            return "No active motorcycle models are configured in the StockSense build catalog yet.";

        var groups = bikeModels
            .OrderBy(model => model.Brand)
            .ThenBy(model => model.Model)
            .GroupBy(model => model.Brand)
            .Take(8)
            .Select(group => $"- {group.Key}: {string.Join(", ", group.Take(8).Select(model => $"{model.Model} ({model.YearStart}-{model.YearEnd})"))}");

        return "These are the active motorcycle models in the StockSense build catalog:\n" + string.Join("\n", groups);
    }

    private static BikeModel? FindBikeModel(string normalizedMessage, List<BikeModel> bikeModels)
    {
        var compactMessage = NormalizeCompact(normalizedMessage);
        var orderedModels = bikeModels
            .OrderByDescending(model => NormalizeCompact(model.DisplayName).Length)
            .ThenByDescending(model => NormalizeCompact(model.Model).Length)
            .ToList();

        return orderedModels.FirstOrDefault(model => compactMessage.Contains(NormalizeCompact(model.DisplayName))) ??
               orderedModels.FirstOrDefault(model => compactMessage.Contains(NormalizeCompact(model.Model)));
    }

    private static List<UpgradePart> SelectTargetCcParts(List<UpgradePart> ccParts, int neededCc)
    {
        var blocks = ccParts
            .Where(part => string.Equals(part.Category?.Name, "Block", StringComparison.OrdinalIgnoreCase))
            .Cast<UpgradePart?>()
            .Append(null)
            .ToList();
        var cranks = ccParts
            .Where(part => string.Equals(part.Category?.Name, "Crank", StringComparison.OrdinalIgnoreCase))
            .Cast<UpgradePart?>()
            .Append(null)
            .ToList();

        var candidates = blocks
            .SelectMany(block => cranks.Select(crank => new[] { block, crank }
                .Where(part => part != null)
                .Cast<UpgradePart>()
                .DistinctBy(part => part.Id)
                .ToList()))
            .Concat(ccParts.Select(part => new List<UpgradePart> { part }))
            .Where(parts => parts.Any())
            .Select(parts => new
            {
                Parts = parts,
                Gain = parts.Sum(part => part.CCGain),
                Price = parts.Sum(part => part.ListPrice)
            })
            .Where(candidate => candidate.Gain > 0)
            .OrderBy(candidate => candidate.Gain >= neededCc ? (candidate.Gain - neededCc) * 3 : neededCc - candidate.Gain)
            .ThenBy(candidate => Math.Abs(candidate.Gain - neededCc))
            .ThenBy(candidate => candidate.Price)
            .FirstOrDefault();

        return candidates?.Parts
            .OrderBy(part => part.Category?.DisplayOrder ?? 0)
            .ThenBy(part => part.Product?.Name)
            .ToList() ?? new();
    }

    private static string BuildMechanicGuidanceWarning(IEnumerable<UpgradePart> parts)
    {
        var hasAdvancedEnginePart = parts.Any(part =>
            string.Equals(part.Category?.Name, "Head", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(part.Category?.Name, "Crank", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(part.Category?.Name, "Block", StringComparison.OrdinalIgnoreCase));

        return hasAdvancedEnginePart
            ? "\nWarning: Head, Crank, and Block upgrades need mechanic guidance before installation because fitment, compression, tuning, and engine safety depend on the exact motorcycle condition."
            : string.Empty;
    }

    private static string FormatCompatibleModels(string compatibleModelsJson, List<BikeModel> bikeModels)
    {
        if (string.IsNullOrWhiteSpace(compatibleModelsJson) || compatibleModelsJson == "[]")
            return "all active build models";

        try
        {
            var ids = JsonSerializer.Deserialize<List<int>>(compatibleModelsJson) ?? new();
            var names = bikeModels
                .Where(model => ids.Contains(model.Id))
                .Select(model => model.DisplayName)
                .ToList();
            if (names.Any())
                return string.Join(", ", names);
        }
        catch
        {
            // Older records may store names instead of IDs.
        }

        try
        {
            var names = JsonSerializer.Deserialize<List<string>>(compatibleModelsJson) ?? new();
            if (names.Any())
                return string.Join(", ", names);
        }
        catch
        {
            // Fall back to the raw text when it is already human readable.
        }

        return compatibleModelsJson;
    }

    private static int ExtractTargetCc(string normalizedMessage)
    {
        var matches = Regex.Matches(normalizedMessage, @"\b(\d{3})\s*cc\b", RegexOptions.IgnoreCase)
            .Select(match => int.Parse(match.Groups[1].Value))
            .ToList();

        return matches.Count == 0 ? 0 : matches.Max();
    }

    private static IEnumerable<UpgradePart> FindUpgradePartsByCategory(string message, List<UpgradePart> upgradeParts)
    {
        var terms = ExtractUsefulTerms(message);
        return upgradeParts
            .Where(part => part.Product != null)
            .Where(part => terms.Any(term =>
                part.Product.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                part.Product.Category.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (part.Category?.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)));
    }

    private static IEnumerable<Product> FindProductsByCategory(string message, List<Product> products)
    {
        var terms = ExtractUsefulTerms(message);
        if (message.Contains("oil", StringComparison.OrdinalIgnoreCase) || message.Contains("oils", StringComparison.OrdinalIgnoreCase))
            terms.Add("oil");

        return products
            .Where(product => terms.Any(term =>
                product.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                product.Brand.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                product.Category.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    private static string ExtractModelHint(string message)
    {
        if (message.Contains("nmax", StringComparison.OrdinalIgnoreCase)) return " for Yamaha NMAX";
        if (message.Contains("aerox", StringComparison.OrdinalIgnoreCase)) return " for Yamaha Aerox";
        if (message.Contains("click", StringComparison.OrdinalIgnoreCase)) return " for Honda Click";
        if (message.Contains("pcx", StringComparison.OrdinalIgnoreCase)) return " for Honda PCX";
        if (message.Contains("adv", StringComparison.OrdinalIgnoreCase)) return " for Honda ADV";
        return string.Empty;
    }

    private static string GetOilModelText(string message)
    {
        if (message.Contains("nmax", StringComparison.OrdinalIgnoreCase)) return "NMAX";
        if (message.Contains("aerox", StringComparison.OrdinalIgnoreCase)) return "Aerox";
        if (message.Contains("click", StringComparison.OrdinalIgnoreCase)) return "Click";
        if (message.Contains("pcx", StringComparison.OrdinalIgnoreCase)) return "PCX";
        if (message.Contains("adv", StringComparison.OrdinalIgnoreCase)) return "ADV";
        return "your motorcycle";
    }

    private static bool IsOilCompatibleWithModel(Product product, string message)
    {
        var isYamahaModel = message.Contains("nmax", StringComparison.OrdinalIgnoreCase) ||
                            message.Contains("aerox", StringComparison.OrdinalIgnoreCase);
        var isHondaModel = message.Contains("click", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("pcx", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("adv", StringComparison.OrdinalIgnoreCase);

        var productText = $"{product.Name} {product.Brand}".ToLowerInvariant();

        if (isYamahaModel)
            return !productText.Contains("honda");

        if (isHondaModel)
            return !productText.Contains("yamaha");

        return true;
    }

    private static bool IsEngineOilProduct(Product product)
    {
        var name = product.Name ?? string.Empty;
        var category = product.Category ?? string.Empty;

        if (name.Contains("oil seal", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("seal oil", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("gear oil", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("fork oil", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("booster", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("additive", StringComparison.OrdinalIgnoreCase))
            return false;

        return name.Contains("oil", StringComparison.OrdinalIgnoreCase) &&
               (category.Contains("maintenance", StringComparison.OrdinalIgnoreCase) ||
                category.Contains("oil", StringComparison.OrdinalIgnoreCase) ||
                category.Contains("engine", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeSearchToken(string token)
    {
        token = token.Trim().ToLowerInvariant();
        return token.Length > 3 && token.EndsWith('s') ? token[..^1] : token;
    }

    private static string NormalizeCompact(string value)
        => string.Concat(value.ToLowerInvariant().Where(char.IsLetterOrDigit));

    private static bool IsGenericInventoryWord(string word)
        => word is "available" or "availability" or "stock" or "price" or "cost" or "have" or "meron" or "presyo" or "magkano"
            or "product" or "item" or "inventory" or "what" or "which" or "show" or "list" or "mga" or "ano" or "available?"
            or "are" or "your" or "now" or "current" or "currently" or "all";

    private static string FormatServices(List<StoreService> services)
        => string.Join("\n", services
            .OrderBy(service => service.Name)
            .Take(10)
            .Select(FormatService));

    private static string FormatService(StoreService service)
    {
        var included = service.RequiredProducts?.Any() == true
            ? $" Includes: {string.Join(", ", service.RequiredProducts.Select(product => product.Name))}."
            : string.Empty;
        return $"- {service.Name}: PHP {service.Price:N0}, about {service.EstimatedMinutes} minutes.{included}";
    }

    private static bool IsCompatibleWithBike(UpgradePart part, BikeModel bike)
    {
        if (string.IsNullOrWhiteSpace(part.CompatibleModelsJson) || part.CompatibleModelsJson == "[]")
            return true;

        try
        {
            var ids = JsonSerializer.Deserialize<List<int>>(part.CompatibleModelsJson);
            if (ids?.Contains(bike.Id) == true) return true;
        }
        catch
        {
            // Some older seed data stored model names instead of catalog IDs.
        }

        try
        {
            var names = JsonSerializer.Deserialize<List<string>>(part.CompatibleModelsJson) ?? new();
            return names.Any(name =>
                name.Contains(bike.Model, StringComparison.OrdinalIgnoreCase) ||
                bike.DisplayName.Contains(name, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return part.CompatibleModelsJson.Contains(bike.Model, StringComparison.OrdinalIgnoreCase) ||
                   part.CompatibleModelsJson.Contains(bike.DisplayName, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static bool IsExhaustPart(UpgradePart part)
    {
        var name = part.Product?.Name ?? string.Empty;
        var category = part.Category?.Name ?? part.Product?.Category ?? string.Empty;
        var brand = part.Product?.Brand ?? string.Empty;
        return name.Contains("exhaust", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("pipe", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("muffler", StringComparison.OrdinalIgnoreCase) ||
               category.Contains("exhaust", StringComparison.OrdinalIgnoreCase) ||
               brand.Contains("pipe", StringComparison.OrdinalIgnoreCase);
    }
}
