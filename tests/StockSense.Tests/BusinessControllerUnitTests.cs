using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Data.Repositories;
using StockSense.Web.Controllers;

namespace StockSense.Tests;

public sealed class BusinessControllerUnitTests
{
    [Fact]
    public async Task Suppliers_Create_TrimsFieldsAndPersistsSupplier()
    {
        await using var db = NewDb();
        var controller = new SuppliersController(db);

        var result = await controller.CreateSupplier(new CreateSupplierDto
        {
            Name = "  Metro Parts  ", Email = " parts@example.test ", MobileNumber = " 09171234567 "
        });

        Assert.IsType<OkObjectResult>(result.Result);
        var supplier = Assert.Single(db.Suppliers);
        Assert.Equal("Metro Parts", supplier.Name);
        Assert.Equal("parts@example.test", supplier.Email);
        Assert.Equal("09171234567", supplier.MobileNumber);
    }

    [Fact]
    public async Task Suppliers_Get_ReturnsAlphabeticalDtos()
    {
        await using var db = NewDb();
        db.Suppliers.AddRange(new Supplier { Name = "Zulu" }, new Supplier { Name = "Alpha" });
        await db.SaveChangesAsync();

        var result = await new SuppliersController(db).GetSuppliers();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(["Alpha", "Zulu"], Assert.IsType<List<SupplierDto>>(ok.Value).Select(x => x.Name));
    }

    [Fact]
    public async Task Mechanics_GetActive_ExcludesInactiveRecords()
    {
        await using var db = NewDb();
        db.Mechanics.AddRange(new Mechanic { Name = "Active", IsActive = true }, new Mechanic { Name = "Inactive", IsActive = false });
        await db.SaveChangesAsync();

        var result = await new MechanicsController(new MechanicRepository(db)).GetActiveMechanics();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("Active", Assert.Single(Assert.IsType<List<MechanicDto>>(ok.Value)).Name);
    }

    [Fact]
    public async Task Mechanics_UpdateMissing_ReturnsNotFound()
    {
        await using var db = NewDb();
        var result = await new MechanicsController(new MechanicRepository(db))
            .UpdateMechanic(404, new MechanicDto { Name = "Nobody", IsActive = true });
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Mechanics_CreateUpdateDelete_RoundTrips()
    {
        await using var db = NewDb();
        var controller = new MechanicsController(new MechanicRepository(db));
        var created = Assert.IsType<OkObjectResult>(await controller.CreateMechanic(new MechanicDto { Name = "Mia", IsActive = true }));
        var dto = Assert.IsType<MechanicDto>(created.Value);
        Assert.IsType<OkResult>(await controller.UpdateMechanic(dto.Id, new MechanicDto { Name = "Mia S", IsActive = false }));
        Assert.False((await db.Mechanics.FindAsync(dto.Id))!.IsActive);
        Assert.IsType<OkObjectResult>(await controller.DeleteMechanic(dto.Id));
        Assert.Empty(db.Mechanics);
    }

    [Fact]
    public async Task Services_Create_SetsActiveAndPersistsFields()
    {
        await using var db = NewDb();
        var controller = Services(db);
        Assert.IsType<OkResult>(await controller.CreateService(new CreateStoreServiceDto
        {
            Name = "Tune up", Category = "Maintenance", Price = 850m, EstimatedMinutes = 75
        }));
        var service = Assert.Single(db.StoreServices);
        Assert.Equal("Active", service.Status);
        Assert.Equal(850m, service.Price);
    }

    [Fact]
    public async Task Services_UpdateProducts_UsesAuthoritativeProductsAndPrice()
    {
        await using var db = NewDb();
        var product = Product("Oil", 320m);
        var service = new StoreService { Name = "Oil change", Category = "Maintenance", Status = "Active" };
        db.AddRange(product, service);
        await db.SaveChangesAsync();

        var result = await Services(db).UpdateServiceProducts(new UpdateServiceProductsDto
        { ServiceId = service.Id, Price = 500m, ProductIds = [product.Id, 999] });

        Assert.IsType<OkResult>(result);
        Assert.Equal(500m, service.Price);
        Assert.Equal(product.Id, Assert.Single(service.RequiredProducts).Id);
    }

    [Fact]
    public async Task Services_UpdateMissing_ReturnsNotFound()
    {
        await using var db = NewDb();
        Assert.IsType<NotFoundObjectResult>(await Services(db).UpdateServiceProducts(
            new UpdateServiceProductsDto { ServiceId = 404 }));
    }

    [Fact]
    public async Task PreBuilt_CreateRejectsEmptyPackage()
    {
        await using var db = NewDb();
        var result = await PreBuilt(db).CreatePreBuilt(new CreatePreBuiltDto { Name = "Empty" });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task PreBuilt_MatchingFiltersInactiveProductsBudgetAndMotorcycle()
    {
        await using var db = NewDb();
        var active = Product("Pipe", 1_000m);
        var inactive = Product("Unsafe", 900m); inactive.IsActive = false;
        db.PreBuiltPackages.AddRange(
            Package("Match", true, active), Package("Inactive package", false, active), Package("Inactive part", true, inactive));
        await db.SaveChangesAsync();

        var result = await PreBuilt(db).GetMatchingPackages("Honda", "Click", "125", 900m, 1_100m);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("Match", Assert.Single(Assert.IsType<List<PreBuiltPackageDto>>(ok.Value)).Name);
    }

    [Fact]
    public async Task PreBuilt_ToggleAndDeleteMissingBehavePredictably()
    {
        await using var db = NewDb();
        var package = Package("Kit", true, Product("Part", 10));
        db.PreBuiltPackages.Add(package); await db.SaveChangesAsync();
        var controller = PreBuilt(db);
        Assert.IsType<OkObjectResult>(await controller.ToggleActive(package.Id));
        Assert.False(package.IsActive);
        Assert.IsType<NotFoundObjectResult>(await controller.DeletePreBuilt(999));
    }

    [Fact]
    public async Task Transactions_FilterMapsItemsAndVoidState()
    {
        await using var db = NewDb();
        db.Transactions.AddRange(
            new Transaction { InvoiceNumber = "S-1", TransactionType = "Sale", IsVoided = true, Items = { new TransactionItem { ProductName = "Oil", Quantity = 2, UnitPrice = 10, LineTotal = 20 } } },
            new Transaction { InvoiceNumber = "R-1", TransactionType = "Restock" });
        await db.SaveChangesAsync();

        var result = await new TransactionController(new TransactionRepository(db)).GetTransactions("Sale");

        var dto = Assert.Single(Assert.IsType<List<TransactionHistoryDto>>(Assert.IsType<OkObjectResult>(result).Value));
        Assert.Equal("S-1", dto.InvoiceNumber);
        Assert.True(dto.IsVoided);
        Assert.Equal(1, dto.ItemCount);
        Assert.Equal(20m, Assert.Single(dto.Items).LineTotal);
    }

    [Fact]
    public async Task Appointment_UpdateProductsRejectsTerminalAndBuildOrders()
    {
        await using var db = NewDb();
        var completed = new Appointment { Status = "Completed", Category = "General" };
        var build = new Appointment { Status = "Pending", Category = "Build" };
        db.AddRange(completed, build); await db.SaveChangesAsync();
        var controller = Appointments(db);
        Assert.IsType<ConflictObjectResult>(await controller.UpdateProducts(completed.Id, new()));
        Assert.IsType<ConflictObjectResult>(await controller.UpdateProducts(build.Id, new()));
    }

    [Fact]
    public async Task Appointment_UpdateProductsPersistsEditableSelection()
    {
        await using var db = NewDb();
        var appointment = new Appointment { Status = "Pending", Category = "General" };
        db.Add(appointment); await db.SaveChangesAsync();

        var result = await Appointments(db).UpdateProducts(appointment.Id,
            new AppointmentsController.UpdateAppointmentProductsDto { SelectedProductsJson = "[]", TotalAmount = 123.45m });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(0m, appointment.TotalAmount);
        Assert.Equal("[]", appointment.SelectedProductsJson);
    }

    [Theory]
    [InlineData(typeof(SuppliersController), "Admin, Employee")]
    [InlineData(typeof(TransactionController), "Employee,Admin")]
    [InlineData(typeof(AppointmentsController), null)]
    [InlineData(typeof(AuthController), null)]
    public void Controllers_DeclareExpectedAuthorization(Type controllerType, string? roles)
    {
        var auth = Assert.Single(controllerType.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(roles, auth.Roles);
    }

    [Fact]
    public void MutatingSupplierAction_IsAdminOnly()
    {
        var method = typeof(SuppliersController).GetMethod(nameof(SuppliersController.CreateSupplier))!;
        Assert.Equal("Admin", Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Roles);
    }

    [Theory]
    [InlineData(typeof(MechanicsController), nameof(MechanicsController.CreateMechanic))]
    [InlineData(typeof(MechanicsController), nameof(MechanicsController.UpdateMechanic))]
    [InlineData(typeof(MechanicsController), nameof(MechanicsController.DeleteMechanic))]
    [InlineData(typeof(PreBuiltController), nameof(PreBuiltController.CreatePreBuilt))]
    [InlineData(typeof(PreBuiltController), nameof(PreBuiltController.UpdatePreBuilt))]
    [InlineData(typeof(PreBuiltController), nameof(PreBuiltController.DeletePreBuilt))]
    [InlineData(typeof(PreBuiltController), nameof(PreBuiltController.ToggleActive))]
    [InlineData(typeof(TransactionController), nameof(TransactionController.VoidTransaction))]
    public void SensitiveMutation_IsAdminOnly(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethods().Single(candidate => candidate.Name == methodName);
        Assert.Equal("Admin", Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Roles);
    }

    [Fact]
    public async Task VoidTransaction_RequiresReason()
    {
        await using var db = NewDb();
        var controller = new TransactionController(new TransactionRepository(db));

        var result = await controller.VoidTransaction(1, new VoidTransactionRequest { Reason = "  " });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void AuthStatus_ReturnsOkAndRequiresAuthentication()
    {
        Assert.IsType<OkResult>(new AuthController().GetStatus());
        Assert.Single(typeof(AuthController).GetCustomAttributes<AuthorizeAttribute>());
    }

    private static ApplicationDbContext NewDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ServicesController Services(ApplicationDbContext db) =>
        new(new StoreServiceRepository(db), new ProductRepository(db));

    private static PreBuiltController PreBuilt(ApplicationDbContext db) => new(new PreBuiltRepository(db));

    private static AppointmentsController Appointments(ApplicationDbContext db) => new(
        new AppointmentRepository(db), new StoreServiceRepository(db), null!, null!, db,
        new MotorcycleRepository(db), NullLogger<AppointmentsController>.Instance);

    private static Product Product(string name, decimal price) => new()
    { Name = name, Category = "Parts", Brand = "Brand", Price = price, IsActive = true };

    private static PreBuiltPackage Package(string name, bool active, Product product) => new()
    {
        Name = name, IsActive = active, IncludedProducts = [product],
        CompatibleMotors = [new PreBuiltPackageMotor { Brand = "Honda", Model = "Click", StockCC = "125" }]
    };
}
