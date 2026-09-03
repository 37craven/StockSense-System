using CsvHelper.Configuration;

namespace StockSense.Infrastructure.Services;

// ── Product CSV Records ──
public class ProductExportRecord
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Barcode { get; set; } = "";
    public decimal Price { get; set; }
    public decimal UnitCost { get; set; }
    public int CurrentStock { get; set; }
    public int ReorderTarget { get; set; }
    public string SupplierName { get; set; } = "";
    public bool IsActive { get; set; }
}

public class ProductImportRecord
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Barcode { get; set; } = "";
    public decimal Price { get; set; }
    public decimal UnitCost { get; set; }
    public int CurrentStock { get; set; }
    public int ReorderTarget { get; set; }
    public string SupplierName { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class ProductExportMap : ClassMap<ProductExportRecord>
{
    public ProductExportMap()
    {
        Map(m => m.Name).Name("Name");
        Map(m => m.Category).Name("Category");
        Map(m => m.Brand).Name("Brand");
        Map(m => m.Barcode).Name("Barcode");
        Map(m => m.Price).Name("Price");
        Map(m => m.UnitCost).Name("UnitCost");
        Map(m => m.CurrentStock).Name("CurrentStock");
        Map(m => m.ReorderTarget).Name("ReorderTarget");
        Map(m => m.SupplierName).Name("Supplier");
        Map(m => m.IsActive).Name("IsActive");
    }
}

public sealed class ProductImportMap : ClassMap<ProductImportRecord>
{
    public ProductImportMap()
    {
        Map(m => m.Name).Name("Name");
        Map(m => m.Category).Name("Category");
        Map(m => m.Brand).Name("Brand").Optional();
        Map(m => m.Barcode).Name("Barcode").Optional();
        Map(m => m.Price).Name("Price");
        Map(m => m.UnitCost).Name("UnitCost").Optional();
        Map(m => m.CurrentStock).Name("CurrentStock");
        Map(m => m.ReorderTarget).Name("ReorderTarget").Optional();
        Map(m => m.SupplierName).Name("Supplier").Optional();
        Map(m => m.IsActive).Name("IsActive").Optional();
    }
}

// ── Supplier CSV Records ──
public class SupplierExportRecord
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string MobileNumber { get; set; } = "";
}

public class SupplierImportRecord
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string MobileNumber { get; set; } = "";
}

public sealed class SupplierExportMap : ClassMap<SupplierExportRecord>
{
    public SupplierExportMap()
    {
        Map(m => m.Name).Name("Name");
        Map(m => m.Email).Name("Email");
        Map(m => m.MobileNumber).Name("MobileNumber");
    }
}

public sealed class SupplierImportMap : ClassMap<SupplierImportRecord>
{
    public SupplierImportMap()
    {
        Map(m => m.Name).Name("Name");
        Map(m => m.Email).Name("Email").Optional();
        Map(m => m.MobileNumber).Name("MobileNumber").Optional();
    }
}

// ── OrderSlip CSV Records ──
public class OrderSlipExportRecord
{
    public string SlipNumber { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public string Status { get; set; } = "";
    public string GeneratedAt { get; set; } = "";
    public string? ApprovedAt { get; set; }
    public string? OrderedAt { get; set; }
    public string? CompletedAt { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public string? Remarks { get; set; }
}

public class OrderSlipItemExportRecord
{
    public string SlipNumber { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Brand { get; set; } = "";
    public int Quantity { get; set; }
    public int OrderedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public decimal UnitCostSnapshot { get; set; }
    public decimal EstimatedLineTotal { get; set; }
}

public sealed class OrderSlipExportMap : ClassMap<OrderSlipExportRecord>
{
    public OrderSlipExportMap()
    {
        Map(m => m.SlipNumber).Name("SlipNumber");
        Map(m => m.SupplierName).Name("Supplier");
        Map(m => m.Status).Name("Status");
        Map(m => m.GeneratedAt).Name("GeneratedAt");
        Map(m => m.ApprovedAt).Name("ApprovedAt");
        Map(m => m.OrderedAt).Name("OrderedAt");
        Map(m => m.CompletedAt).Name("CompletedAt");
        Map(m => m.TotalEstimatedCost).Name("TotalEstimatedCost");
        Map(m => m.Remarks).Name("Remarks");
    }
}

public sealed class OrderSlipItemExportMap : ClassMap<OrderSlipItemExportRecord>
{
    public OrderSlipItemExportMap()
    {
        Map(m => m.SlipNumber).Name("SlipNumber");
        Map(m => m.ProductName).Name("Product");
        Map(m => m.Brand).Name("Brand");
        Map(m => m.Quantity).Name("Quantity");
        Map(m => m.OrderedQuantity).Name("OrderedQuantity");
        Map(m => m.ReceivedQuantity).Name("ReceivedQuantity");
        Map(m => m.UnitCostSnapshot).Name("UnitCost");
        Map(m => m.EstimatedLineTotal).Name("LineTotal");
    }
}

// ── Transaction CSV Records ──
public class TransactionExportRecord
{
    public string InvoiceNumber { get; set; } = "";
    public string TransactionDate { get; set; } = "";
    public string TransactionType { get; set; } = "";
    public string SaleSource { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ServiceAmount { get; set; }
    public bool IsVoided { get; set; }
    public string? Remarks { get; set; }
}

public class TransactionItemExportRecord
{
    public string InvoiceNumber { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class TransactionExportMap : ClassMap<TransactionExportRecord>
{
    public TransactionExportMap()
    {
        Map(m => m.InvoiceNumber).Name("InvoiceNumber");
        Map(m => m.TransactionDate).Name("Date");
        Map(m => m.TransactionType).Name("Type");
        Map(m => m.SaleSource).Name("SaleSource");
        Map(m => m.PaymentMethod).Name("PaymentMethod");
        Map(m => m.TotalAmount).Name("TotalAmount");
        Map(m => m.DiscountAmount).Name("Discount");
        Map(m => m.ServiceAmount).Name("ServiceAmount");
        Map(m => m.IsVoided).Name("IsVoided");
        Map(m => m.Remarks).Name("Remarks");
    }
}

public sealed class TransactionItemExportMap : ClassMap<TransactionItemExportRecord>
{
    public TransactionItemExportMap()
    {
        Map(m => m.InvoiceNumber).Name("InvoiceNumber");
        Map(m => m.ProductName).Name("Product");
        Map(m => m.Quantity).Name("Quantity");
        Map(m => m.UnitPrice).Name("UnitPrice");
        Map(m => m.LineTotal).Name("LineTotal");
    }
}

// ── Import Validation Result ──
public class CsvImportPreview<T>
{
    public List<T> ValidRows { get; set; } = new();
    public List<CsvImportError> Errors { get; set; } = new();
    public int TotalRows { get; set; }
    public int ValidCount => ValidRows.Count;
    public int ErrorCount => Errors.Count;
}

public class CsvImportError
{
    public int Row { get; set; }
    public string Field { get; set; } = "";
    public string Message { get; set; } = "";
}
