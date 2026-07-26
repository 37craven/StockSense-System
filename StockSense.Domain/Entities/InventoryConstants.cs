namespace StockSense.Domain.Entities;

public static class InventoryDefaults
{
    public const string LocationId = "MAIN";
    public const string CalculationVersion = "1.0";
}

public static class InventoryCalculationModes
{
    public const string Auto = "Auto";
    public const string Manual = "Manual";
}

public static class InventoryCalculationStages
{
    public const string ColdStart = "ColdStart";
    public const string Learning = "Learning";
    public const string DataDriven = "DataDriven";
    public const string Manual = "Manual";
}

public static class InventoryConfidenceLevels
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
}

public static class TransactionTypes
{
    public const string Sale = "Sale";
    public const string Purchase = "Purchase";
    public const string PurchaseReceipt = "PurchaseReceipt";
    public const string CustomerReturn = "CustomerReturn";
    public const string SupplierReturn = "SupplierReturn";
    public const string Adjustment = "Adjustment";
    public const string Damage = "Damage";
    public const string TransferIn = "TransferIn";
    public const string TransferOut = "TransferOut";
    public const string StockCorrection = "StockCorrection";
}

public static class OrderSlipStatuses
{
    public const string Draft = "Draft";
    public const string Approved = "Approved";
    public const string Ordered = "Ordered";
    public const string PartiallyReceived = "PartiallyReceived";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}

public static class WorkOrderStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}
