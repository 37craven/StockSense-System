using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Data.Repositories;
using StockSense.Infrastructure.Services;
using StockSense.Web.Controllers;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;

namespace StockSense.Tests;

public sealed class ProductImageUploadValidationTests
{
    [Fact]
    public async Task UploadProductImage_PersistsAndReplacesOwnedImage()
    {
        var webRoot = Path.Combine(Path.GetTempPath(), $"stocksense-image-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "products"));
        try
        {
            await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
            var product = new Product { Name = "Upload test", Category = "Test", Price = 1, RowVersion = [1, 2, 3] };
            context.Products.Add(product);
            await context.SaveChangesAsync();
            var oldName = $"product-{product.Id}-{new string('a', 32)}.png";
            product.ImageUrl = $"/uploads/products/{oldName}";
            await context.SaveChangesAsync();
            var oldPath = Path.Combine(webRoot, "uploads", "products", oldName);
            await File.WriteAllBytesAsync(oldPath, [1]);
            var controller = CreateController(context);
            var imageBytes = await ValidPngAsync(24, 24);

            var action = await controller.UploadProductImage(
                product.Id,
                FormFile(imageBytes, "product.png", "image/png"),
                Convert.ToBase64String(product.RowVersion),
                new TestEnvironment(webRoot),
                CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(action);
            var result = Assert.IsType<ProductImageUploadResultDto>(ok.Value);
            Assert.StartsWith("/uploads/products/product-", result.ImageUrl, StringComparison.Ordinal);
            Assert.False(File.Exists(oldPath));
            Assert.True(File.Exists(Path.Combine(webRoot, result.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
            Assert.Equal(result.ImageUrl, (await context.Products.FindAsync(product.Id))!.ImageUrl);
        }
        finally
        {
            if (Directory.Exists(webRoot)) Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task UploadProductImage_RejectsOversizedFileBeforeDatabaseAccess()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);
        var file = FormFile(new byte[5 * 1024 * 1024 + 1], "large.png", "image/png");

        var result = await controller.UploadProductImage(1, file, "", null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadProductImage_RejectsSignatureOnlyCorruptImageBeforeDatabaseAccess()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);
        var fakePng = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };
        var file = FormFile(fakePng, "corrupt.png", "image/png");

        var result = await controller.UploadProductImage(1, file, "", null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadProductImage_RejectsExcessiveDecodedDimensionBeforeDatabaseAccess()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);
        using var image = new Image<Rgba32>(8193, 1);
        using var bytes = new MemoryStream();
        await image.SaveAsPngAsync(bytes);
        var file = FormFile(bytes.ToArray(), "wide.png", "image/png");

        var result = await controller.UploadProductImage(1, file, "", null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static FormFile FormFile(byte[] bytes, string fileName, string contentType) =>
        new(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

    private static async Task<byte[]> ValidPngAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var bytes = new MemoryStream();
        await image.SaveAsPngAsync(bytes);
        return bytes.ToArray();
    }

    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=StockSense-ImageValidation-NotUsed;Trusted_Connection=True")
            .Options);

    private static ProductsController CreateController(ApplicationDbContext context) =>
        new(
            new ProductRepository(context),
            new EmailSender(new ConfigurationBuilder().Build()),
            new BarcodeService(),
            context,
            new SafetyStockCalculationService(context, NullLogger<SafetyStockCalculationService>.Instance),
            NullLogger<ProductsController>.Instance);

    private sealed class TestEnvironment(string webRoot) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "StockSense.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = webRoot;
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = webRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
