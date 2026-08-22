using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StockSense.Domain.Entities;
using StockSense.Application.DTOs;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Data.Repositories;
using StockSense.Infrastructure.Services;
using StockSense.Web.Controllers;

namespace StockSense.Tests;

public sealed class BarcodePdfTests
{
    [Fact]
    public async Task CreateProduct_rejects_a_duplicate_name_ignoring_case_and_outer_whitespace()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        context.Products.Add(new Product { Name = "Premium Oil Filter", Category = "Filters" });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.CreateProduct(new CreateProductDto
        {
            Name = "  premium OIL filter  ",
            Brand = "Test",
            Category = "Filters",
            Price = 100
        });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(context.Products);
    }

    [Fact]
    public async Task GetBarcodePdf_RegeneratesInvalidLegacyBarcode()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        var product = new Product
        {
            Name = "Legacy product",
            Category = "Test",
            Barcode = "INVALID-CODE!"
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.GetBarcodePdf(product.Id);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.NotEmpty(file.FileContents);
        Assert.True(BarcodeService.IsValidEan13(product.Barcode));
    }

    private static ProductsController CreateController(ApplicationDbContext context)
    {
        var controller = new ProductsController(
            new ProductRepository(context),
            new EmailSender(new ConfigurationBuilder().Build()),
            new BarcodeService(),
            context,
            new SafetyStockCalculationService(context, NullLogger<SafetyStockCalculationService>.Instance),
            NullLogger<ProductsController>.Instance);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    [Theory]
    [InlineData("2000000000015", true)]
    [InlineData("2000000000014", false)]
    [InlineData("ABCDEFGHIJKLM", false)]
    [InlineData(null, false)]
    public void IsValidEan13_ValidatesDigitsAndCheckDigit(string? value, bool expected)
    {
        Assert.Equal(expected, BarcodeService.IsValidEan13(value));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(999999)]
    public void GenerateBarcodeValue_IsDeterministicUniqueAndValid(int productId)
    {
        var first = BarcodeService.GenerateBarcodeValue(productId);
        var second = BarcodeService.GenerateBarcodeValue(productId);

        Assert.Equal(first, second);
        Assert.StartsWith("20", first, StringComparison.Ordinal);
        Assert.True(BarcodeService.IsValidEan13(first));
        Assert.NotEqual(first, BarcodeService.GenerateBarcodeValue(productId + 1));
    }

    [Fact]
    public void GenerateBarcodeImage_ReturnsPngPayload()
    {
        var service = new BarcodeService();
        var product = new Product { Id = 7, Name = "Filter", Barcode = BarcodeService.GenerateBarcodeValue(7) };

        var barcode = service.GenerateBarcodeImage(product.Barcode);

        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, barcode[..4]);
    }

    [Fact]
    public void GenerateBarcodeLabelPdf_RejectsNullBarcodeImage()
    {
        var service = new BarcodeService();
        var product = new Product { Name = "Filter", Category = "Test" };

        Assert.Throws<ArgumentNullException>(
            () => service.GenerateBarcodeLabelPdf(product, null!));
    }

    [Fact]
    public void PdfDownloadCache_ReturnsPayloadExactlyOnce()
    {
        var cache = new PdfDownloadCache();
        byte[] payload = [1, 2, 3];

        var token = cache.Store(payload);

        Assert.Equal(payload, cache.Retrieve(token));
        Assert.Null(cache.Retrieve(token));
        Assert.Null(cache.Retrieve("missing-token"));
    }
}
