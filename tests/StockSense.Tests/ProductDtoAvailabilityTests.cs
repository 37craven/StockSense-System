using StockSense.Application.DTOs;

namespace StockSense.Tests;

public sealed class ProductDtoAvailabilityTests
{
    [Theory]
    [InlineData(10, 3, 7)]
    [InlineData(2, 2, 0)]
    [InlineData(1, 5, 0)]
    public void AvailableStock_SubtractsReservationsAndNeverReturnsNegative(
        int currentStock,
        int reservedStock,
        int expected)
    {
        var product = new ProductDto(
            1, "Part", CurrentStock: currentStock, ReservedStock: reservedStock);

        Assert.Equal(expected, product.AvailableStock);
    }

    [Fact]
    public void AvailableStock_IsZeroForInactiveProduct()
    {
        var product = new ProductDto(
            1, "Discontinued Part", CurrentStock: 10, IsActive: false, ReservedStock: 2);

        Assert.Equal(0, product.AvailableStock);
    }
}
