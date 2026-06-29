namespace StockSense.Application.DTOs;

public class ReceiptDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<ReceiptItemDto> Items { get; set; } = new();
}

public class ReceiptItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
