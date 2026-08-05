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
}
