using StockSense.Application.DTOs;
using StockSense.Client.Components;

namespace StockSense.Tests;

public sealed class CustomerBuildCatalogPolicyTests
{
    [Fact]
    public void SelectProducts_ReturnsOnlyActiveProducts()
    {
        var products = new[]
        {
            Product(1, isActive: true),
            Product(2, isActive: false)
        };

        var result = CustomerBuildCatalogPolicy.SelectProducts(products);

        Assert.Equal([1], result.Select(product => product.Id));
    }

    [Fact]
    public void SelectPackages_RequiresActivePackageAndActiveIncludedProducts()
    {
        var packages = new[]
        {
            Package(1, isActive: true, ProductItem(11, isActive: true)),
            Package(2, isActive: false, ProductItem(12, isActive: true)),
            Package(3, isActive: true, ProductItem(13, isActive: false))
        };

        var result = CustomerBuildCatalogPolicy.SelectPackages(packages);

        Assert.Equal([1], result.Select(package => package.Id));
    }

    [Fact]
    public void CanSelect_RejectsInactiveProductsAndPackages()
    {
        Assert.False(CustomerBuildCatalogPolicy.CanSelect(Product(1, isActive: false)));
        Assert.False(CustomerBuildCatalogPolicy.CanSelect(
            Package(2, isActive: true, ProductItem(3, isActive: false))));
    }

    private static ProductDto Product(int id, bool isActive) =>
        new(id, $"Product {id}", IsActive: isActive);

    private static PreBuiltProductDto ProductItem(int id, bool isActive) => new()
    {
        Id = id,
        Name = $"Product {id}",
        IsActive = isActive
    };

    private static PreBuiltPackageDto Package(int id, bool isActive, params PreBuiltProductDto[] products) => new()
    {
        Id = id,
        Name = $"Package {id}",
        IsActive = isActive,
        IncludedProducts = products.ToList()
    };
}
