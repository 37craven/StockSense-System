using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Data.Repositories;
using StockSense.Infrastructure.Services;
using StockSense.Web.Controllers;

namespace StockSense.Tests;

public sealed class BarcodePdfTests
{
    [Fact]
    public async Task GetBarcodePdf_QrOnly_SucceedsWithInvalidLegacyBarcode()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        var product = new Product
        {
            Name = "Legacy QR product",
            Category = "Test",
            Barcode = "INVALID-CODE!"
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var controller = new ProductsController(
            new ProductRepository(context),
            new EmailSender(new ConfigurationBuilder().Build()),
            new BarcodeService(),
            context,
            new SafetyStockCalculationService(context, NullLogger<SafetyStockCalculationService>.Instance),
            NullLogger<ProductsController>.Instance);

        var result = await controller.GetBarcodePdf(product.Id, "qr");

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.NotEmpty(file.FileContents);
        Assert.Equal("INVALID-CODE!", product.Barcode);
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
    public void GenerateBarcodeAndQrImages_ReturnPngPayloads()
    {
        var service = new BarcodeService();
        var product = new Product { Id = 7, Name = "Filter", Barcode = BarcodeService.GenerateBarcodeValue(7) };

        var barcode = service.GenerateBarcodeImage(product.Barcode);
        var qr = service.GenerateQrCodeImage(product);

        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, barcode[..4]);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, qr[..4]);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    public void GenerateBarcodeLabelPdf_RejectsUnsupportedFormat(string format)
    {
        var service = new BarcodeService();
        var product = new Product { Name = "Filter", Category = "Test" };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => service.GenerateBarcodeLabelPdf(product, null, null, format));
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
