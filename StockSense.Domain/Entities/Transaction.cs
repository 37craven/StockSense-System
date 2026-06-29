using System.ComponentModel.DataAnnotations.Schema;
namespace StockSense.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
    public List<TransactionItem> Items { get; set; } = new();
}