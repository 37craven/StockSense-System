namespace StockSense.Application.DTOs;

public class TransactionHistoryDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Remarks { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public bool IsVoided { get; set; }
    public List<TransactionHistoryItemDto> Items { get; set; } = new();
}

public class TransactionHistoryItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }
}
