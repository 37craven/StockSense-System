using StockSense.Application.DTOs;

namespace StockSense.Tests;

public sealed class ProductDtoAvailabilityTests
{
    [Theory]
    [InlineData(10, 10)]
    [InlineData(2, 2)]
    [InlineData(1, 1)]
    public void AvailableStock_SubtractsReservationsAndNeverReturnsNegative(
        int currentStock,
        int expected)
    {
        var product = new ProductDto(
            1, "Part", CurrentStock: currentStock);

        Assert.Equal(expected, product.AvailableStock);
    }

    [Fact]
    public void AvailableStock_IsZeroForInactiveProduct()
    {
        var product = new ProductDto(
            1, "Discontinued Part", CurrentStock: 10, IsActive: false);

        Assert.Equal(0, product.AvailableStock);
    }
}
