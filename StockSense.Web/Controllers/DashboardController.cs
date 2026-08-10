using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Application.DTOs;
using StockSense.Infrastructure.Data.Repositories;

namespace StockSense.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin, Employee")]
public class DashboardController : ControllerBase
{
    private readonly ProductRepository _productRepo;
    private readonly OrderSlipRepository _orderSlipRepo;

    public DashboardController(ProductRepository productRepo, OrderSlipRepository orderSlipRepo)
    {
        _productRepo = productRepo;
        _orderSlipRepo = orderSlipRepo;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var allProducts = await _productRepo.GetAllAsync();
        var pendingOrders = await _orderSlipRepo.GetPendingCountAsync();

        var dto = new
        {
            TotalProducts = allProducts.Count,
            LowStockCount = allProducts.Count(p => p.CurrentStock <= p.ReorderTarget),
            TotalValue = allProducts.Sum(p => p.Price * p.CurrentStock),
            PendingOrders = pendingOrders,
            LowStockProducts = allProducts
                .Where(p => p.CurrentStock <= p.ReorderTarget)
                .OrderBy(p => p.CurrentStock)
                .Take(5)
                .Select(p => new ProductDto(
                    p.Id, p.Name, p.Category, p.Brand, p.Price, p.CurrentStock,
                    p.ReorderTarget, p.SupplierId ?? 0, p.Supplier?.Name ?? "", p.ImageUrl ?? "",
                    IsActive: p.IsActive, ReservedStock: p.ReservedStock))
                .ToList()
        };

        return Ok(dto);
    }
}
