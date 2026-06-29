namespace StockSense.Application.DTOs;

// Changed to a class with { get; set; } so Blazor can edit the Quantity!
public class OrderSlipItemDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderTarget { get; set; }
    
    
    // Because this has a "set;", the red line in Blazor will disappear
    public int Quantity { get; set; } 
    public int ReceivedQuantity { get; set; }
}

// We change the parent to a class too, just in case you need to edit it later
public class OrderSlipDto
{
    public int Id { get; set; }
    public string SlipNumber { get; set; } = string.Empty;
    public DateTime DateGenerated { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierEmail { get; set; } = string.Empty;
    public bool IsReceived { get; set; }
    public List<OrderSlipItemDto> Items { get; set; } = new();
}