using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data.Repositories;

namespace StockSense.Web.Controllers;

[Authorize(Roles = "Employee,Admin")]
[ApiController]
[Route("api/transactions")]
public class TransactionController : ControllerBase
{
    private readonly TransactionRepository _repo;

    public TransactionController(TransactionRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions([FromQuery] string? type)
    {
        var transactions = await _repo.GetFilteredAsync(type);
        var dtos = transactions.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    private static TransactionHistoryDto MapToDto(Transaction t) => new()
    {
        Id = t.Id,
        InvoiceNumber = t.InvoiceNumber,
        TransactionDate = t.TransactionDate,
        TransactionType = t.TransactionType,
        PaymentMethod = t.PaymentMethod,
        ReferenceNumber = t.ReferenceNumber,
        Remarks = t.Remarks,
        TotalAmount = t.TotalAmount,
        ItemCount = t.Items.Count,
        IsVoided = t.IsVoided,
        Items = t.Items.Select(i => new TransactionHistoryItemDto
        {
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.LineTotal,
            StockBefore = i.StockBefore,
            StockAfter = i.StockAfter
        }).ToList()
    };
}
