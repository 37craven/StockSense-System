using System.ComponentModel.DataAnnotations.Schema;
namespace StockSense.Domain.Entities;

public class TransactionItem
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}