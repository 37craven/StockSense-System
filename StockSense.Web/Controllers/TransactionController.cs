using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data.Repositories;
using System.Security.Claims;

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

    [HttpPost("{id:int}/void")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> VoidTransaction(int id, [FromBody] VoidTransactionRequest request)
    {
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
            return BadRequest(ApiResponse.Error("Please enter a reason for voiding this transaction."));
        if (reason.Length > 300)
            return BadRequest(ApiResponse.Error("The void reason must be 300 characters or fewer."));

        var transaction = await _repo.GetByIdWithItemsAsync(id);
        if (transaction is null) return NotFound(ApiResponse.NotFound("Transaction"));
        if (transaction.IsVoided)
            return Conflict(ApiResponse.Error("This transaction has already been voided."));
        if (transaction.TransactionType != TransactionTypes.Sale ||
            !transaction.InvoiceNumber.StartsWith("TXN-", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Error("Only completed point-of-sale transactions can be voided here."));

        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _repo.VoidSaleAsync(id, reason, adminUserId);
        return Ok(ApiResponse.Success("The transaction was voided and its stock was restored."));
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

public sealed class VoidTransactionRequest
{
    public string Reason { get; set; } = string.Empty;
}
