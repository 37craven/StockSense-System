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
    [Column(TypeName = "decimal(18,2)")] public decimal UnitCost { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DiscountAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal LineTotal { get; set; }
    public int Quantity { get; set; }
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }
    public int? RequestedQuantity { get; set; }
    public int LostSalesQuantity { get; set; }
    public bool StockoutOccurred { get; set; }
    public int? OrderSlipItemId { get; set; }
    public OrderSlipItem? OrderSlipItem { get; set; }
}
