using System.Text;
using System.Security.Claims;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Data.Repositories;
using StockSense.Infrastructure.Services;
using SixLabors.ImageSharp;

namespace StockSense.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ProductRepository _productRepo;
    private readonly EmailSender _emailSender;
    private readonly BarcodeService _barcodeService;
    private readonly ApplicationDbContext _context;
    private readonly ISafetyStockCalculationService _calculationService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        ProductRepository productRepo,
        EmailSender emailSender,
        BarcodeService barcodeService,
        ApplicationDbContext context,
        ISafetyStockCalculationService calculationService,
        ILogger<ProductsController> logger)
    {
        _productRepo = productRepo;
        _emailSender = emailSender;
        _barcodeService = barcodeService;
        _context = context;
        _calculationService = calculationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetProducts()
    {
        var products = await _productRepo.GetAllAsync();
        var dtos = products.Select(p => new ProductDto(p.Id, p.Name, p.Category, p.Brand, p.Price, p.CurrentStock, p.ReorderTarget, p.SupplierId ?? 0, p.Supplier?.Name ?? "", p.ImageUrl ?? "", p.Barcode, p.UnitCost, p.RowVersion)).ToList();
        return Ok(dtos);
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ProductDto>> GetProductByBarcode(string barcode)
    {
        var product = await _productRepo.GetByBarcodeAsync(barcode);
        if (product == null) return NotFound(ApiResponse.NotFound("Product"));

        var dto = new ProductDto(product.Id, product.Name, product.Category, product.Brand, product.Price,
            product.CurrentStock, product.ReorderTarget, product.SupplierId ?? 0, product.Supplier?.Name ?? "",
            product.ImageUrl ?? "", product.Barcode, product.UnitCost, product.RowVersion);
        return Ok(dto);
    }

    [HttpPost("send-quote")]
    public async Task<IActionResult> SendQuote([FromBody] EmailQuoteRequest request)
    {
        var products = await _productRepo.GetAllAsync();
        var selectedProducts = products.Where(p => request.ProductIds.Contains(p.Id)).ToList();
        if (!selectedProducts.Any()) return BadRequest(ApiResponse.Error("No valid products found."));

        decimal grandTotal = selectedProducts.Sum(p => p.Price);

        var sb = new StringBuilder();
        sb.AppendLine("<h1>StockSense Build Quotation</h1>");
        sb.AppendLine($"<p>Hello {request.UserEmail}, here is the quote for your custom build:</p>");
        sb.AppendLine("<table border='1' cellpadding='10' cellspacing='0' style='border-collapse:collapse; width:100%; text-align:left;'>");
        sb.AppendLine("<tr style='background-color:#f2f2f2;'><th>Part Name</th><th>Category</th><th>Price</th></tr>");

        foreach (var p in selectedProducts)
        {
            sb.AppendLine($"<tr><td>{p.Name}</td><td>{p.Category}</td><td>P {p.Price:N2}</td></tr>");
        }
        sb.AppendLine("</table>");
        sb.AppendLine($"<h3>Grand Total: P {grandTotal:N2}</h3>");

        try
        {
            await _emailSender.SendEmailAsync(request.UserEmail, "Custom Build Quote", sb.ToString());
            return Ok(new { message = "Email sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending a product quotation failed.");
            return StatusCode(500, ApiResponse.Error("The quotation could not be sent. Please try again later."));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Brand = dto.Brand,
            Category = dto.Category,
            Price = dto.Price,
            UnitCost = dto.UnitCost,
            ReorderTarget = dto.ReorderTarget,
            SupplierId = dto.SupplierId,
            ImageUrl = dto.ImageUrl
        };
        if (dto.InitialStock > 0) product.AddStock(dto.InitialStock);
        await _productRepo.AddAsync(product);
        await _productRepo.SaveChangesAsync(); // first save to get the auto-generated Id

        // Every new product gets a unique, system-generated barcode - staff never type one in.
        product.Barcode = BarcodeService.GenerateBarcodeValue(product.Id);
        await _productRepo.SaveChangesAsync();

        var dtoResult = new ProductDto(product.Id, product.Name, product.Category, product.Brand, product.Price,
            product.CurrentStock, product.ReorderTarget, product.SupplierId ?? 0, product.Supplier?.Name ?? "",
            product.ImageUrl ?? "", product.Barcode, product.UnitCost, product.RowVersion);
        return Ok(dtoResult);
    }

    [HttpGet("{id}/barcode-pdf")]
    public async Task<IActionResult> GetBarcodePdf(int id, [FromQuery] string format = "both")
    {
        var product = await _productRepo.GetByIdAsync(id);
        if (product == null) return NotFound(ApiResponse.NotFound("Product"));

        if (string.IsNullOrWhiteSpace(product.Barcode) || product.Barcode.Length != 13)
        {
            product.Barcode = BarcodeService.GenerateBarcodeValue(product.Id);
            await _productRepo.SaveChangesAsync();
        }

        var barcodePng = _barcodeService.GenerateBarcodeImage(product.Barcode);
        var qrPng = _barcodeService.GenerateQrCodeImage(product);
        var pdfBytes = _barcodeService.GenerateBarcodeLabelPdf(product, barcodePng, qrPng, format);
        var safeName = string.Concat(product.Name.Split(Path.GetInvalidFileNameChars()));
        var prefix = format == "qr" ? "QR_" : "Barcode_";
        return File(pdfBytes, "application/pdf", $"{prefix}{safeName}.pdf");
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto dto)
    {
        if (id != dto.Id) return BadRequest(ApiResponse.Error("ID mismatch."));

        var product = await _productRepo.GetByIdAsync(id);
        if (product == null) return NotFound(ApiResponse.NotFound("Product"));

        var newBarcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim();
        if (newBarcode != null)
        {
            var existing = await _productRepo.GetByBarcodeAsync(newBarcode);
            if (existing != null && existing.Id != id) return BadRequest(ApiResponse.Error("A product with this barcode already exists."));
        }

        var changesStock = dto.CurrentStock.HasValue && dto.CurrentStock.Value != product.CurrentStock;
        var changesReorderTarget = dto.ReorderTarget.HasValue && dto.ReorderTarget.Value != product.ReorderTarget;
        var changesInventory = changesStock || changesReorderTarget;
        if (changesInventory && dto.RowVersion.Length == 0)
            return BadRequest(ApiResponse.Error("A row version is required when changing stock or the reorder target. Reload the product and try again."));

        if (changesInventory)
            _context.Entry(product).Property(value => value.RowVersion).OriginalValue = dto.RowVersion;

        var stockBefore = product.CurrentStock;
        product.Barcode = newBarcode;
        product.Price = dto.Price;
        product.UnitCost = dto.UnitCost;
        if (dto.ReorderTarget.HasValue) product.ReorderTarget = dto.ReorderTarget.Value;
        if (dto.CurrentStock.HasValue) product.CurrentStock = dto.CurrentStock.Value;

        if (changesStock)
        {
            var changedAt = DateTime.Now;
            _context.Transactions.Add(new Transaction
            {
                InvoiceNumber = $"ADJ-{changedAt:yyMMdd-HHss}-{InvoiceHelper.ShortCode()}",
                TransactionDate = changedAt,
                TransactionType = TransactionTypes.StockCorrection,
                PaymentMethod = "N/A",
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                LocationId = InventoryDefaults.LocationId,
                ReferenceNumber = $"PRODUCT-{product.Id}",
                Remarks = "Product stock corrected through the product administration endpoint.",
                Items =
                [
                    new TransactionItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        UnitPrice = product.Price,
                        UnitCost = product.UnitCost,
                        Quantity = Math.Abs(product.CurrentStock - stockBefore),
                        StockBefore = stockBefore,
                        StockAfter = product.CurrentStock
                    }
                ]
            });
        }

        try
        {
            await _productRepo.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(exception, "Product {ProductId} update conflicted with another inventory change.", id);
            return Conflict(ApiResponse.Error("The product was changed by another user. Reload the latest data and try again."));
        }

        if (!changesStock) return NoContent();

        try
        {
            await _calculationService.RecalculateProductAsync(product.Id, InventoryDefaults.LocationId, HttpContext.RequestAborted);
            return NoContent();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Stock correction for product {ProductId} committed, but safety-stock recalculation did not complete.",
                product.Id);
            return Ok(new
            {
                message = "Product updated.",
                warning = "Safety-stock metrics could not be refreshed. Run recalculation again from inventory management."
            });
        }
    }

    [HttpPut("{id:int}/inventory-values")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProductInventory(
        int id, [FromBody] UpdateProductInventoryDto dto, CancellationToken cancellationToken)
    {
        if (id != dto.Id) return BadRequest(ApiResponse.Error("ID mismatch."));
        if (dto.Price <= 0) return BadRequest(ApiResponse.Error("Price must be greater than zero."));
        if (decimal.Round(dto.Price, 2) != dto.Price)
            return BadRequest(ApiResponse.Error("Price cannot have more than two decimal places."));
        if (dto.ProductRowVersion.Length == 0)
            return BadRequest(ApiResponse.Error("A row version is required. Reload the product and try again."));

        var product = await _context.Products.SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (product is null) return NotFound(ApiResponse.NotFound("Product"));

        int resultingStock;
        try { resultingStock = checked(product.CurrentStock + dto.StockAdjustment); }
        catch (OverflowException) { return BadRequest(ApiResponse.Error("The resulting stock quantity is outside the supported range.")); }
        if (resultingStock < 0 || resultingStock > 999_999)
            return BadRequest(ApiResponse.Error("The resulting stock must be between 0 and 999,999."));
        var reason = dto.Reason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reason))
            return BadRequest(ApiResponse.Error("A reason is required for inventory value changes."));
        if (reason.Length > 500)
            return BadRequest(ApiResponse.Error("The reason cannot exceed 500 characters."));
        if (dto.Price == product.Price && dto.StockAdjustment == 0)
            return BadRequest(ApiResponse.Error("No price or stock changes were supplied."));

        var oldPrice = product.Price;
        var valueAudit = string.Format(
            CultureInfo.InvariantCulture,
            "Price {0:0.00} -> {1:0.00}; Reason: ",
            oldPrice, dto.Price);
        var auditRemarks = valueAudit + reason;
        if (auditRemarks.Length > 500)
            return BadRequest(ApiResponse.Error(
                $"The reason is too long for the audit record. Use {500 - valueAudit.Length} characters or fewer."));

        _context.Entry(product).Property(value => value.RowVersion).OriginalValue = dto.ProductRowVersion;
        var stockBefore = product.CurrentStock;
        product.Price = dto.Price;

        if (dto.StockAdjustment > 0) product.AddStock(dto.StockAdjustment);
        else if (dto.StockAdjustment < 0) product.DeductStock(-dto.StockAdjustment);

        var changedAt = DateTime.Now;
        var adjustment = new Transaction
        {
            InvoiceNumber = $"ADJ-{changedAt:yyMMdd-HHss}-{InvoiceHelper.ShortCode()}",
            TransactionDate = changedAt,
            TransactionType = dto.StockAdjustment == 0 ? TransactionTypes.Adjustment : TransactionTypes.StockCorrection,
            PaymentMethod = "N/A",
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            LocationId = InventoryDefaults.LocationId,
            ReferenceNumber = $"PRODUCT-{product.Id}",
            Remarks = auditRemarks
        };
        if (dto.StockAdjustment != 0)
        {
            adjustment.Items.Add(
                new TransactionItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                UnitCost = product.UnitCost,
                Quantity = Math.Abs(dto.StockAdjustment),
                StockBefore = stockBefore,
                StockAfter = product.CurrentStock
            });
        }
        _context.Transactions.Add(adjustment);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(exception, "Product {ProductId} inventory edit conflicted with another change.", id);
            return Conflict(ApiResponse.Error("The product was changed by another user. Reload the latest data and try again."));
        }

        string? warning = null;
        if (dto.StockAdjustment != 0)
        {
            try
            {
                await _calculationService.RecalculateProductAsync(
                    product.Id, InventoryDefaults.LocationId, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception,
                    "Stock correction for product {ProductId} committed, but safety-stock recalculation did not complete.",
                    product.Id);
                warning = "Stock and price were saved, but safety-stock metrics could not be refreshed. Run recalculation again.";
            }
        }

        return Ok(new UpdateProductInventoryResultDto(
            product.Id, product.Price, product.CurrentStock, product.RowVersion, warning));
    }

    [HttpPost("{id:int}/image")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadProductImage(
        int id,
        [FromForm] IFormFile file,
        [FromForm] string rowVersion,
        [FromServices] IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        const long maxBytes = 5 * 1024 * 1024;
        if (file is null || file.Length == 0) return BadRequest(ApiResponse.Error("Choose an image to upload."));
        if (file.Length > maxBytes) return BadRequest(ApiResponse.Error("The image cannot exceed 5 MB."));
        var contentType = file.ContentType.ToLowerInvariant();
        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => null
        };
        var suppliedExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var extensionMatches = contentType == "image/jpeg"
            ? suppliedExtension is ".jpg" or ".jpeg"
            : suppliedExtension == extension;
        if (extension is null || !extensionMatches)
            return BadRequest(ApiResponse.Error("Upload a JPEG, PNG, or WebP image with a matching file extension."));
        var imageValidationError = await ValidateImageAsync(file, extension, cancellationToken);
        if (imageValidationError is not null) return BadRequest(ApiResponse.Error(imageValidationError));
        byte[] version;
        try { version = Convert.FromBase64String(rowVersion ?? string.Empty); }
        catch (FormatException) { return BadRequest(ApiResponse.Error("The product version is invalid. Reload and try again.")); }
        if (version.Length == 0) return BadRequest(ApiResponse.Error("A product version is required. Reload and try again."));

        var product = await _context.Products.SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (product is null) return NotFound(ApiResponse.NotFound("Product"));
        _context.Entry(product).Property(value => value.RowVersion).OriginalValue = version;
        var previousImageUrl = product.ImageUrl;
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var uploadDirectory = Path.Combine(webRoot, "uploads", "products");
        Directory.CreateDirectory(uploadDirectory);
        var fileName = $"product-{id}-{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadDirectory, fileName);
        try
        {
            await using (var target = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                await file.CopyToAsync(target, cancellationToken);
            product.ImageUrl = $"/uploads/products/{fileName}";
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            TryDeleteFile(fullPath, $"new image after a concurrency conflict for product {id}");
            return Conflict(ApiResponse.Error("The product was changed by another user. Reload the latest data and try again."));
        }
        catch
        {
            TryDeleteFile(fullPath, $"new image after a failed upload for product {id}");
            throw;
        }
        DeleteOwnedProductImage(previousImageUrl, uploadDirectory, id);
        return Ok(new ProductImageUploadResultDto(product.Id, product.ImageUrl, product.RowVersion));
    }

    private static async Task<string?> ValidateImageAsync(IFormFile file, string extension, CancellationToken cancellationToken)
    {
        const int maxDimension = 8192;
        const long maxPixels = 40_000_000;
        try
        {
            await using var source = file.OpenReadStream();
            var info = await Image.IdentifyAsync(source, cancellationToken);
            if (info is null) return "The uploaded file is not a readable image.";
            var formatExtensions = info.Metadata.DecodedImageFormat?.FileExtensions ?? [];
            var normalizedExtension = extension.TrimStart('.');
            if (!formatExtensions.Contains(normalizedExtension, StringComparer.OrdinalIgnoreCase)
                && !(normalizedExtension == "jpg" && formatExtensions.Contains("jpeg", StringComparer.OrdinalIgnoreCase)))
                return "The image content does not match its declared format.";
            if (info.Width < 1 || info.Height < 1 || info.Width > maxDimension || info.Height > maxDimension
                || checked((long)info.Width * info.Height) > maxPixels)
                return $"Image dimensions cannot exceed {maxDimension:N0} pixels per side or {maxPixels:N0} total pixels.";
            source.Position = 0;
            using var decoded = await Image.LoadAsync(source, cancellationToken);
            if (decoded.Width != info.Width || decoded.Height != info.Height)
                return "The image dimensions are inconsistent.";
            return null;
        }
        catch (UnknownImageFormatException) { return "The uploaded file is not a valid JPEG, PNG, or WebP image."; }
        catch (InvalidImageContentException) { return "The image is corrupt or incomplete."; }
        catch (NotSupportedException) { return "The image format is not supported."; }
    }

    private void DeleteOwnedProductImage(string? imageUrl, string uploadDirectory, int productId)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/uploads/products/", StringComparison.Ordinal)) return;
        var basename = imageUrl["/uploads/products/".Length..];
        if (!System.Text.RegularExpressions.Regex.IsMatch(basename, @"^product-\d+-[a-f0-9]{32}\.(jpg|png|webp)$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)) return;
        if (!string.Equals(Path.GetFileName(basename), basename, StringComparison.Ordinal)) return;
        var root = Path.GetFullPath(uploadDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(uploadDirectory, basename));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return;
        TryDeleteFile(fullPath, $"previous image for product {productId}");
    }

    private void TryDeleteFile(string fullPath, string description)
    {
        try { if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "Database operation succeeded, but cleanup of {Description} failed at {Path}.", description, fullPath);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int id, [FromServices] IWebHostEnvironment environment)
    {
        var product = await _productRepo.GetByIdAsync(id);
        if (product == null) return NotFound(ApiResponse.NotFound("Product"));

        var previousImageUrl = product.ImageUrl;
        await _productRepo.DeleteAsync(product);
        await _productRepo.SaveChangesAsync();
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        DeleteOwnedProductImage(previousImageUrl, Path.Combine(webRoot, "uploads", "products"), product.Id);
        return Ok();
    }

    public class EmailQuoteRequest
    {
        public string UserEmail { get; set; } = "";
        public List<int> ProductIds { get; set; } = new();
    }
}
