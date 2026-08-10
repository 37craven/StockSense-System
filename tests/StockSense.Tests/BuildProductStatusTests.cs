using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Data.Repositories;
using StockSense.Web.Controllers;

namespace StockSense.Tests;

public sealed class BuildProductStatusTests
{
    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 999)]
    public async Task CreateBuild_rejects_inactive_or_missing_products(bool active, int submittedId)
    {
        await using var fixture = await Fixture.CreateAsync(active);

        var result = await fixture.Controller.CreateBuild(fixture.Command(submittedId, 1m, "Client value"));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("no longer available", badRequest.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await fixture.Context.BuildRequests.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task CreateBuild_uses_authoritative_product_snapshot_and_total()
    {
        await using var fixture = await Fixture.CreateAsync(active: true);

        var result = await fixture.Controller.CreateBuild(fixture.Command(1, 1m, "Forged name"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<BuildRequestDto>(ok.Value);
        Assert.Equal(250m, response.TotalPrice);
        var savedParts = JsonSerializer.Deserialize<List<ProductDto>>(response.SelectedPartsJson)!;
        var product = Assert.Single(savedParts.Where(part => part.Id > 0));
        Assert.Equal("Trusted product", product.Name);
        Assert.Equal(250m, product.Price);
        Assert.True(product.IsActive);
        Assert.Contains(savedParts, part => part.Id == -999 && part.Name == "TYPE_CUSTOM");
    }

    [Fact]
    public async Task CreateBuild_rejects_duplicate_parts_when_available_stock_is_too_low()
    {
        await using var fixture = await Fixture.CreateAsync(active: true, currentStock: 2, reservedStock: 1);
        var command = fixture.Command(1, 1m, "Client value");
        var parts = JsonSerializer.Deserialize<List<ProductDto>>(command.SelectedPartsJson)!;
        parts.Insert(1, parts[0]);
        command.SelectedPartsJson = JsonSerializer.Serialize(parts);

        var result = await fixture.Controller.CreateBuild(command);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("Trusted product", conflict.Value!.ToString());
        Assert.DoesNotContain("available:", conflict.Value!.ToString());
        Assert.DoesNotContain("needed:", conflict.Value!.ToString());
        Assert.Empty(await fixture.Context.BuildRequests.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task CreateAppointment_rejects_out_of_stock_selected_parts_without_creating_booking()
    {
        await using var fixture = await Fixture.CreateAsync(active: true, currentStock: 1, reservedStock: 1);
        var controller = new AppointmentsController(
            new AppointmentRepository(fixture.Context),
            new StoreServiceRepository(fixture.Context),
            new CheckoutStub(),
            fixture.UserManager,
            fixture.Context,
            new MotorcycleRepository(fixture.Context),
            NullLogger<AppointmentsController>.Instance)
        {
            ControllerContext = fixture.Controller.ControllerContext
        };
        var selectedProducts = JsonSerializer.Serialize(new[]
        {
            new
            {
                ServiceName = "Oil change",
                ServicePrice = 0m,
                Products = new[] { new { Id = 1, Name = "Client value", Price = 1m, Selected = true } }
            }
        });

        var result = await controller.Create(new CreateAppointmentDto
        {
            AppointmentDate = DateTime.Today.AddDays(1),
            TimeSlot = "10:00",
            SelectedServices = [],
            SelectedProductsJson = selectedProducts
        });

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("Trusted product", conflict.Value!.ToString());
        Assert.DoesNotContain("available:", conflict.Value!.ToString());
        Assert.DoesNotContain("needed:", conflict.Value!.ToString());
        Assert.Empty(await fixture.Context.Appointments.AsNoTracking().ToListAsync());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TestUserStore _store;

        private Fixture(ApplicationDbContext context, UserManager<ApplicationUser> userManager, TestUserStore store)
        {
            Context = context;
            _userManager = userManager;
            _store = store;
            Controller = new BuildsController(
                new BuildRequestRepository(context),
                new CheckoutStub(),
                userManager,
                context,
                new MotorcycleRepository(context),
                NullLogger<BuildsController>.Instance)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            [new Claim(ClaimTypes.NameIdentifier, "customer-1")], "Test"))
                    }
                }
            };
        }

        public ApplicationDbContext Context { get; }
        public BuildsController Controller { get; }
        public UserManager<ApplicationUser> UserManager => _userManager;

        public static async Task<Fixture> CreateAsync(bool active, int currentStock = 10, int reservedStock = 0)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"build-product-status-{Guid.NewGuid():N}")
                .Options;
            var context = new ApplicationDbContext(options);
            context.Products.Add(new Product
            {
                Id = 1,
                Name = "Trusted product",
                Category = "Engine",
                Brand = "StockSense",
                Price = 250m,
                CurrentStock = currentStock,
                ReservedStock = reservedStock,
                IsActive = active
            });
            await context.SaveChangesAsync();

            var user = new ApplicationUser
            {
                Id = "customer-1",
                UserName = "customer@example.com",
                Email = "customer@example.com",
                FirstName = "Test",
                LastName = "Customer"
            };
            var store = new TestUserStore(user);
            var manager = new UserManager<ApplicationUser>(
                store,
                Options.Create(new IdentityOptions()),
                new PasswordHasher<ApplicationUser>(),
                [],
                [],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new ServiceCollection().BuildServiceProvider(),
                NullLogger<UserManager<ApplicationUser>>.Instance);
            return new Fixture(context, manager, store);
        }

        public CreateBuildRequestDto Command(int id, decimal submittedPrice, string submittedName)
        {
            var parts = new List<ProductDto>
            {
                new(id, submittedName, Price: submittedPrice),
                new(-999, "TYPE_CUSTOM", "SYSTEM_METADATA")
            };
            return new CreateBuildRequestDto
            {
                BuildName = "Test build",
                SelectedPartsJson = JsonSerializer.Serialize(parts),
                TotalPrice = submittedPrice
            };
        }

        public async ValueTask DisposeAsync()
        {
            _userManager.Dispose();
            _store.Dispose();
            await Context.DisposeAsync();
        }
    }

    private sealed class TestUserStore(ApplicationUser user) : IUserStore<ApplicationUser>
    {
        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id == userId ? user : null);
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            Task.FromResult<ApplicationUser?>(null);
        public Task<string> GetUserIdAsync(ApplicationUser value, CancellationToken cancellationToken) => Task.FromResult(value.Id);
        public Task<string?> GetUserNameAsync(ApplicationUser value, CancellationToken cancellationToken) => Task.FromResult(value.UserName);
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser value, CancellationToken cancellationToken) => Task.FromResult(value.NormalizedUserName);
        public Task SetUserNameAsync(ApplicationUser value, string? userName, CancellationToken cancellationToken) { value.UserName = userName; return Task.CompletedTask; }
        public Task SetNormalizedUserNameAsync(ApplicationUser value, string? normalizedName, CancellationToken cancellationToken) { value.NormalizedUserName = normalizedName; return Task.CompletedTask; }
        public Task<IdentityResult> CreateAsync(ApplicationUser value, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> UpdateAsync(ApplicationUser value, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> DeleteAsync(ApplicationUser value, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public void Dispose() { }
    }

    private sealed class CheckoutStub : IWorkOrderCheckoutService
    {
        public Task<ReceiptDto> CompleteAppointmentAsync(int appointmentId, CompleteWorkOrderDto request, string? employeeUserId, string locationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ReceiptDto> CompleteBuildAsync(int buildId, CompleteWorkOrderDto request, string? employeeUserId, string locationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
