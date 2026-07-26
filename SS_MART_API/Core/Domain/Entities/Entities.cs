namespace SS_MART_API.Core.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Version { get; set; } = 1;
    public string SyncStatus { get; set; } = "pending";
    public DateTime? DeletedAt { get; set; }
}

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public string HSNCode { get; set; } = string.Empty;
    public string Unit { get; set; } = "PCS";
    public double PackSize { get; set; } = 1.0;
    public decimal MRP { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal TaxRate { get; set; }
    public string TaxType { get; set; } = "GST";
    public Guid? CategoryId { get; set; }
    public Guid? SupplierId { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public int CurrentStock { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public string? GSTIN { get; set; }
    public string Type { get; set; } = "B2C";
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public int LoyaltyPoints { get; set; }
    public string? LoyaltyCardNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Bill : BaseEntity
{
    public string BillNumber { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTime BillDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public string TaxRuleVersion { get; set; } = "v1";
    public decimal DiscountAmount { get; set; }
    public decimal RoundOff { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public string PaymentMode { get; set; } = "CASH";
    public string Status { get; set; } = "completed";
    public bool IsReturn { get; set; }
    public Guid? ReferenceBillId { get; set; }
    public Guid CreatedBy { get; set; }

    public virtual Customer? Customer { get; set; }
    public virtual ICollection<BillItem> Items { get; set; } = new List<BillItem>();
}

public class BillItem : BaseEntity
{
    public Guid BillId { get; set; }
    public Guid ProductId { get; set; }
    public double Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public string TaxRuleVersion { get; set; } = "v1";
    public decimal TotalAmount { get; set; }
    public string? BatchNumber { get; set; }

    public virtual Bill Bill { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

public class Stock : BaseEntity
{
    public Guid ProductId { get; set; }
    public string LocationId { get; set; } = "MAIN";
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime LastUpdated { get; set; }

    public virtual Product Product { get; set; } = null!;
}

public class StockMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }

    public virtual Product Product { get; set; } = null!;
}

public class LoyaltyTransaction : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int Points { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}

public class Employee : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = "cashier";
    public string? Pin { get; set; }
    public Guid? StoreId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Attendance : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public DateTime ClockInTime { get; set; }
    public DateTime? ClockOutTime { get; set; }
    public double? HoursWorked { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}

public class SyncQueue : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
}

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public string? DeviceId { get; set; }
}

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
}

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? GSTIN { get; set; }
    public string? PAN { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public string PaymentTerms { get; set; } = "net30";
    public bool IsActive { get; set; } = true;
}

public class PurchaseOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "draft";
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }

    public virtual Supplier Supplier { get; set; } = null!;
    public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}

public class PurchaseOrderItem : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public double Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public double? ReceivedQuantity { get; set; }
    public string? BatchNumber { get; set; }

    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

public class SalesOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "draft";
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }

    public virtual Customer? Customer { get; set; }
    public virtual ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
}

public class SalesOrderItem : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public Guid ProductId { get; set; }
    public double Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public double? DeliveredQuantity { get; set; }

    public virtual SalesOrder SalesOrder { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

public class DeliveryChallan : BaseEntity
{
    public string ChallanNumber { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public DateTime ChallanDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? VehicleNumber { get; set; }
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public string Status { get; set; } = "pending";
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }

    public virtual Customer? Customer { get; set; }
    public virtual SalesOrder? SalesOrder { get; set; }
    public virtual ICollection<DeliveryChallanItem> Items { get; set; } = new List<DeliveryChallanItem>();
}

public class DeliveryChallanItem : BaseEntity
{
    public Guid DeliveryChallanId { get; set; }
    public Guid ProductId { get; set; }
    public double Quantity { get; set; }
    public double? DeliveredQuantity { get; set; }
    public string? Unit { get; set; }

    public virtual DeliveryChallan DeliveryChallan { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

public class LedgerEntry : BaseEntity
{
    public DateTime EntryDate { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string AccountHead { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public Guid CreatedBy { get; set; }
}

public class LabelTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "barcode";
    public int Width { get; set; } = 58;
    public int Height { get; set; } = 30;
    public string? Layout { get; set; }
    public bool IsDefault { get; set; }
}

public class Setting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
}
