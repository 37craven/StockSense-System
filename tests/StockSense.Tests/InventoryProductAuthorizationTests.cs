using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using StockSense.Web.Controllers;

namespace StockSense.Tests;

public sealed class InventoryProductAuthorizationTests
{
    [Fact]
    public void InventoryController_AllowsOnlyAdminsAndEmployees()
    {
        var authorization = Assert.Single(typeof(InventoryController).GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal("Admin, Employee", authorization.Roles);
    }

    [Fact]
    public void Dashboard_UsesInventoryStaffAuthorizationAndExpectedRoute()
    {
        var method = typeof(InventoryController).GetMethod(nameof(InventoryController.GetDashboard))!;

        Assert.Empty(method.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("dashboard", Assert.Single(method.GetCustomAttributes<HttpGetAttribute>()).Template);
    }

    [Fact]
    public void RecalculationWrites_UseInventoryStaffAuthorization()
    {
        AssertInventoryStaffPost(nameof(InventoryController.RecalculateProduct), "recalculate/{productId:int}");
        AssertInventoryStaffPost(nameof(InventoryController.RecalculateSelected), "recalculate-selected");
        AssertInventoryStaffPost(nameof(InventoryController.RecalculateAll), "recalculate-all");
    }

    [Fact]
    public void InventorySettingsUpdate_IsAdminOnly()
    {
        var method = typeof(InventoryController).GetMethod(nameof(InventoryController.UpdateSettings))!;

        Assert.Equal("Admin", Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Roles);
        Assert.Equal("products/{productId:int}/settings", Assert.Single(method.GetCustomAttributes<HttpPutAttribute>()).Template);
    }

    [Fact]
    public void ProductCatalogWrites_AreAdminOnly()
    {
        AssertAdminOnly(nameof(ProductsController.CreateProduct), typeof(HttpPostAttribute), null);
        AssertAdminOnly(nameof(ProductsController.UpdateProductStatus), typeof(HttpPutAttribute), "{id:int}/status");
        AssertAdminOnly(nameof(ProductsController.UpdateProduct), typeof(HttpPutAttribute), "{id}");
        AssertAdminOnly(nameof(ProductsController.UpdateProductInventory), typeof(HttpPutAttribute), "{id:int}/inventory-values");
        AssertAdminOnly(nameof(ProductsController.UploadProductImage), typeof(HttpPostAttribute), "{id:int}/image");
        AssertAdminOnly(nameof(ProductsController.DeleteProduct), typeof(HttpDeleteAttribute), "{id}");
    }

    private static void AssertInventoryStaffPost(string methodName, string route)
    {
        var method = typeof(InventoryController).GetMethod(methodName)!;
        Assert.Empty(method.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(route, Assert.Single(method.GetCustomAttributes<HttpPostAttribute>()).Template);
    }

    private static void AssertAdminOnly(string methodName, Type httpAttributeType, string? route)
    {
        var method = typeof(ProductsController).GetMethod(methodName)!;
        Assert.Equal("Admin", Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Roles);

        var httpAttribute = Assert.Single(method.GetCustomAttributes(httpAttributeType, inherit: true));
        Assert.Equal(route, Assert.IsAssignableFrom<IRouteTemplateProvider>(httpAttribute).Template);
    }
}
