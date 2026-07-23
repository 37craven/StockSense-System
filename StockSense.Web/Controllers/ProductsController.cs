using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Application.DTOs;
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

    public ProductsController(ProductRepository productRepo, EmailSender emailSender)
    {
        _productRepo = productRepo;
        _emailSender = emailSender;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetProducts()
    {
        var products = await _productRepo.GetAllAsync();
        var dtos = products.Select(p => new ProductDto(p.Id, p.Name, p.Category, p.Brand, p.Price, p.CurrentStock, p.ReorderTarget, p.SupplierId ?? 0, p.Supplier?.Name ?? "", p.ImageUrl ?? "", p.Barcode)).ToList();
        return Ok(dtos);
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ProductDto>> GetProductByBarcode(string barcode)
    {
        var product = await _productRepo.GetByBarcodeAsync(barcode);
        if (product == null) return NotFound(ApiResponse.NotFound("Product"));

        var dto = new ProductDto(product.Id, product.Name, product.Category, product.Brand, product.Price,
            product.CurrentStock, product.ReorderTarget, product.SupplierId ?? 0, product.Supplier?.Name ?? "",
            product.ImageUrl ?? "", product.Barcode);
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
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.Barcode))
        {
            var existing = await _productRepo.GetByBarcodeAsync(dto.Barcode.Trim());
            if (existing != null) return BadRequest(ApiResponse.Error("A product with this barcode already exists."));
        }

        var product = new Product
        {
            Name = dto.Name,
            Brand = dto.Brand,
            Category = dto.Category,
            Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim(),
            Price = dto.Price,
            ReorderTarget = dto.ReorderTarget,
            ImageUrl = dto.ImageUrl
        };
        if (dto.InitialStock > 0) product.AddStock(dto.InitialStock);
        await _productRepo.AddAsync(product);
        await _productRepo.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("{id}")]
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

        product.Barcode = newBarcode;
        product.Price = dto.Price;
        product.ReorderTarget = dto.ReorderTarget;
        product.CurrentStock = dto.CurrentStock;
        await _productRepo.UpdateAsync(product);
        await _productRepo.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
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