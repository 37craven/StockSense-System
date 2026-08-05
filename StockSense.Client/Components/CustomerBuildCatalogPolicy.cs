using StockSense.Application.DTOs;

namespace StockSense.Client.Components;

public static class CustomerBuildCatalogPolicy
{
    public static List<ProductDto> SelectProducts(IEnumerable<ProductDto> products) =>
        products.Where(product => product.IsActive).ToList();

    public static List<PreBuiltPackageDto> SelectPackages(IEnumerable<PreBuiltPackageDto> packages) =>
        packages
            .Where(package => package.IsActive && package.IncludedProducts.All(product => product.IsActive))
            .ToList();

    public static bool CanSelect(ProductDto product) => product.IsActive;

    public static bool CanSelect(PreBuiltPackageDto package) =>
        package.IsActive && package.IncludedProducts.All(product => product.IsActive);
}
