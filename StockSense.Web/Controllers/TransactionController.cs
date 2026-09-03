using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Application.DTOs;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data.Repositories;
using StockSense.Infrastructure.Services;
using System.Security.Claims;

namespace StockSense.Web.Controllers;

[Authorize(Roles = "Employee,Admin")]
[ApiController]
[Route("api/transactions")]
public class TransactionController : ControllerBase
{
    private readonly TransactionRepository _repo;
    private readonly DocumentService _documents;

    public TransactionController(TransactionRepository repo, DocumentService documents)
    {
        _repo = repo;
        _documents = documents;
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions([FromQuery] string? type)
    {
        var transactions = await _repo.GetFilteredAsync(type);
        var dtos = transactions.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportTransactions([FromQuery] string? type)
    {
        var transactions = await _repo.GetFilteredAsync(type);
        var records = transactions.Select(t => new TransactionExportRecord
        {
            InvoiceNumber = t.InvoiceNumber,
            TransactionDate = t.TransactionDate.ToString("yyyy-MM-dd HH:mm"),
            TransactionType = t.TransactionType,
            SaleSource = GetSaleSource(t.InvoiceNumber),
            PaymentMethod = t.PaymentMethod,
            TotalAmount = t.TotalAmount,
            DiscountAmount = t.DiscountAmount,
            ServiceAmount = t.ServiceAmount,
            IsVoided = t.IsVoided,
            Remarks = t.Remarks
        }).ToList();

        var bytes = CsvService.ExportToCsv(records, new TransactionExportMap());
        return File(bytes, "text/csv", $"transactions_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("export/items")]
    public async Task<IActionResult> ExportTransactionItems([FromQuery] string? type)
    {
        var transactions = await _repo.GetFilteredAsync(type);
        var records = transactions.SelectMany(t => t.Items.Select(i => new TransactionItemExportRecord
        {
            InvoiceNumber = t.InvoiceNumber,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.LineTotal
        })).ToList();

        var bytes = CsvService.ExportToCsv(records, new TransactionItemExportMap());
        return File(bytes, "text/csv", $"transaction_items_{DateTime.Now:yyyyMMdd}.csv");
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

    [HttpGet("{id:int}/receipt-pdf")]
    public async Task<IActionResult> DownloadReceiptPdf(int id, [FromQuery] bool inline = false)
    {
        var transaction = await _repo.GetByIdWithItemsAsync(id);
        if (transaction is null) return NotFound();

        var dto = new ReceiptDto
        {
            Id = transaction.Id,
            InvoiceNumber = transaction.InvoiceNumber,
            TransactionDate = transaction.TransactionDate,
            TransactionType = transaction.TransactionType,
            PaymentMethod = transaction.PaymentMethod,
            ReferenceNumber = transaction.ReferenceNumber,
            Remarks = transaction.Remarks,
            DiscountAmount = transaction.DiscountAmount,
            TotalAmount = transaction.TotalAmount,
            Items = transaction.Items.Select(i => new ReceiptItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                DiscountAmount = i.DiscountAmount,
                LineTotal = i.LineTotal
            }).ToList()
        };

        var bytes = _documents.GenerateTransactionReceiptPdf(dto);
        return inline
            ? File(bytes, "application/pdf")
            : File(bytes, "application/pdf", $"Receipt_{transaction.InvoiceNumber}.pdf");
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

    private static string GetSaleSource(string invoice) => invoice switch
    {
        { Length: > 3 } when invoice.StartsWith("APT", StringComparison.OrdinalIgnoreCase) => "Appointment",
        { Length: > 3 } when invoice.StartsWith("BLD", StringComparison.OrdinalIgnoreCase) => "Build",
        _ => "POS"
    };
}

public sealed class VoidTransactionRequest
{
    public string Reason { get; set; } = string.Empty;
}
