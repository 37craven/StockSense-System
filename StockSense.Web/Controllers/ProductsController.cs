using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Data.Repositories;
using StockSense.Infrastructure.Services;

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
            return StatusCode(500, ApiResponse.Error(ex.Message));
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
    public async Task<IActionResult> GetBarcodePdf(int id)
    {
        var product = await _productRepo.GetByIdAsync(id);
        if (product == null) return NotFound(ApiResponse.NotFound("Product"));

        // Safety net for products that existed before this feature and have no barcode yet.
        if (string.IsNullOrWhiteSpace(product.Barcode))
        {
            product.Barcode = BarcodeService.GenerateBarcodeValue(product.Id);
            await _productRepo.SaveChangesAsync();
        }

        var barcodePng = _barcodeService.GenerateBarcodeImage(product.Barcode);
        var qrPng = _barcodeService.GenerateQrCodeImage(product);
        var pdfBytes = _barcodeService.GenerateBarcodeLabelPdf(product, barcodePng, qrPng);
        var safeName = string.Concat(product.Name.Split(Path.GetInvalidFileNameChars()));
        return File(pdfBytes, "application/pdf", $"Barcode_{safeName}.pdf");
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
                InvoiceNumber = $"ADJ-{changedAt:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}",
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

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _productRepo.GetByIdAsync(id);
        if (product == null) return NotFound(ApiResponse.NotFound("Product"));

        await _productRepo.DeleteAsync(product);
        await _productRepo.SaveChangesAsync();
        return Ok();
    }

    public class EmailQuoteRequest
    {
        public string UserEmail { get; set; } = "";
        public List<int> ProductIds { get; set; } = new();
    }
}
