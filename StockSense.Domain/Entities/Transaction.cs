using System.ComponentModel.DataAnnotations.Schema;
namespace StockSense.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(80)]
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    [System.ComponentModel.DataAnnotations.MaxLength(20)] public string TransactionType { get; set; } = TransactionTypes.Sale;
    [System.ComponentModel.DataAnnotations.MaxLength(20)] public string PaymentMethod { get; set; } = "Cash";
    [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? ReferenceNumber { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(450)] public string? UserId { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(50)] public string LocationId { get; set; } = "MAIN";
    [System.ComponentModel.DataAnnotations.MaxLength(500)] public string? Remarks { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DiscountAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ServiceAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
    public int? OrderSlipId { get; set; }
    public OrderSlip? OrderSlip { get; set; }
    public List<TransactionItem> Items { get; set; } = new();
}
