using System.Text.Json;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data.Repositories;
using StockSense.Infrastructure.Services;
using StockSense.Application.Interfaces;

namespace StockSense.Web.Helpers;

// ponytail: concrete helpers, no interfaces — each has one consumer (razor pages)
public class OrderSlipHelper
{
    private readonly OrderSlipRepository _repo;
    private readonly ProductRepository _productRepo;
    private readonly DocumentService _docService;
    private readonly IOrderEmailSender _orderEmailSender;
    private readonly PinnedSlipRepository _pinnedRepo;

    public OrderSlipHelper(OrderSlipRepository repo, ProductRepository productRepo,
        DocumentService docService, IOrderEmailSender orderEmailSender, PinnedSlipRepository pinnedRepo)
    {
        _repo = repo; _productRepo = productRepo; _docService = docService;
        _orderEmailSender = orderEmailSender; _pinnedRepo = pinnedRepo;
    }

    public async Task<int> GetPendingCountAsync() => await _repo.GetPendingCountAsync();

    public async Task<List<OrderSlipDto>> GenerateSuggestedOrderSlipsAsync()
    {
        var products = await _productRepo.GetAllAsync();
        var lowStockProducts = products.Where(p => p.CurrentStock < p.ReorderTarget).ToList();
        var generatedSlips = new List<OrderSlip>();
        int slipCounter = 1;

        foreach (var group in lowStockProducts.GroupBy(p => p.SupplierId))
        {
            var supplier = group.First().Supplier;
            if (supplier == null) continue;
            generatedSlips.Add(new OrderSlip
            {
                SlipNumber = $"ORD-{DateTime.Now.Year}-{slipCounter:D3}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                SupplierId = supplier.Id, Supplier = supplier, DateGenerated = DateTime.Now,
                Items = group.Select(p => new OrderSlipItem
                {
                    ProductName = p.Name, Brand = p.Brand, Category = p.Category,
                    CurrentStock = p.CurrentStock, ReorderTarget = p.ReorderTarget,
                    Quantity = Math.Max(p.ReorderTarget - p.CurrentStock, 5)
                }).ToList()
            });
            slipCounter++;
        }
        return generatedSlips.Select(MapToDto).ToList();
    }

    public async Task<bool> SaveOrderSlipToDbAsync(SaveOrderSlipCommand command)
    {
        var supplierId = command.SupplierId;
        if (supplierId <= 0)
        {
            var allProducts = await _productRepo.GetAllAsync();
            var matched = allProducts.FirstOrDefault(p =>
                command.Items.Any(i => i.ProductName == p.Name && i.Brand == p.Brand));
            if (matched?.SupplierId is int matchedSupplierId && matchedSupplierId > 0)
                supplierId = matchedSupplierId;
        }

        var newSlip = new OrderSlip
        {
            SlipNumber = command.SlipNumber, DateGenerated = DateTime.Now, SupplierId = supplierId,
            Items = command.Items.Select(i => new OrderSlipItem
            { ProductName = i.ProductName, Brand = i.Brand, Quantity = i.Quantity }).ToList()
        };

        await _repo.AddSlipAsync(newSlip);
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<List<OrderSlipDto>> GetSavedOrderSlipsAsync()
    {
        var slips = await _repo.GetAllAsync();
        return slips.Select(MapToDto).ToList();
    }

    public async Task<bool> MarkAsReceivedAsync(MarkAsReceivedCommand command)
    {
        var dbSlip = await _repo.GetByIdAsync(command.SlipId);
        if (dbSlip == null || dbSlip.IsReceived) return false;

        foreach (var itemCmd in command.Items)
        {
            if (itemCmd.ReceivedQuantity <= 0) continue;
            var dtoItem = dbSlip.Items.FirstOrDefault(i => i.Id == itemCmd.ItemId);
            if (dtoItem == null) continue;

            var product = (await _productRepo.GetAllAsync())
                .FirstOrDefault(p => p.Name == dtoItem.ProductName && p.Brand == dtoItem.Brand);
            if (product != null) product.AddStock(itemCmd.ReceivedQuantity);

            dbSlip.ReceiveItem(itemCmd.ItemId, itemCmd.ReceivedQuantity);
        }

        dbSlip.MarkAsReceived();
        await _repo.UpdateSlipAsync(dbSlip);
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteOrderSlipAsync(int id)
    {
        var slip = await _repo.GetByIdAsync(id);
        if (slip == null) return false;
        await _repo.DeleteAsync(id);
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveItemFromSlipAsync(int itemId)
    {
        await _repo.DeleteItemAsync(itemId);
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<List<OrderSlipDto>> GenerateSingleProductSlipAsync(int productId)
    {
        var allProducts = await _productRepo.GetAllAsync();
        var p = allProducts.FirstOrDefault(pr => pr.Id == productId);
        if (p == null) return new List<OrderSlipDto>();

        var slip = new OrderSlip
        {
            SlipNumber = $"ORD-SNGL-{Guid.NewGuid().ToString()[..4].ToUpper()}",
            SupplierId = p.SupplierId ?? 0, Supplier = p.Supplier!, DateGenerated = DateTime.Now,
            Items = new List<OrderSlipItem>
            {
                new() { ProductName = p.Name, Brand = p.Brand, Quantity = Math.Max(p.ReorderTarget - p.CurrentStock, 10), Reasoning = "On-demand single product reorder." }
            }
        };
        return new List<OrderSlipDto> { MapToDto(slip) };
    }

    public byte[] GeneratePdfBytes(OrderSlipDto slipDto) => _docService.GenerateOrderSlipPdf(slipDto);

    public async Task<bool> SendEmailAsync(string recipientEmail, byte[] pdfAttachment, string slipNumber)
    {
        string subject = $"Purchase Order - {slipNumber}";
        string body = PurchaseOrderEmailTemplate.Build(slipNumber);
        await _orderEmailSender.SendEmailWithAttachmentAsync(recipientEmail, subject, body, pdfAttachment, $"Order_{slipNumber}.pdf");
        return true;
    }

    public async Task ApplyProductToItemAsync(OrderSlipItemDto item, int productId)
    {
        var products = await _productRepo.GetAllAsync();
        var product = products.FirstOrDefault(p => p.Id == productId);
        if (product == null) return;
        item.ProductName = product.Name; item.Brand = product.Brand; item.Category = product.Category;
        item.CurrentStock = product.CurrentStock; item.ReorderTarget = product.ReorderTarget;
        if (item.Quantity == 0) item.Quantity = product.ReorderTarget;
    }

    public async Task<List<OrderSlipDto>?> GetPinnedSlipsAsync(string userId)
    {
        var json = await _pinnedRepo.GetSlipDataAsync(userId);
        if (string.IsNullOrEmpty(json)) return null;
        return JsonSerializer.Deserialize<List<OrderSlipDto>>(json);
    }

    public async Task SavePinnedSlipsAsync(string userId, List<OrderSlipDto> slips)
    {
        var json = JsonSerializer.Serialize(slips);
        await _pinnedRepo.SetSlipDataAsync(userId, json);
    }

    private static OrderSlipDto MapToDto(OrderSlip slip) => new()
    {
        Id = slip.Id, SlipNumber = slip.SlipNumber, DateGenerated = slip.DateGenerated,
        SupplierId = slip.SupplierId, SupplierName = slip.Supplier?.Name ?? "Unknown Supplier",
        SupplierEmail = slip.Supplier?.Email ?? "", IsReceived = slip.IsReceived,
        Items = slip.Items.Select(i => new OrderSlipItemDto
        {
            Id = i.Id, ProductName = i.ProductName, Brand = i.Brand,
            Category = i.Category ?? "Uncategorized", CurrentStock = i.CurrentStock,
            ReorderTarget = i.ReorderTarget, Quantity = i.Quantity, ReceivedQuantity = i.ReceivedQuantity
        }).ToList()
    };
}
