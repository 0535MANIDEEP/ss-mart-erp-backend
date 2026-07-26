namespace SS_MART_API.Core.Domain.Enums;

public enum EntityType
{
    Product,
    Customer,
    Bill,
    BillItem,
    Stock,
    StockMovement,
    LoyaltyTransaction,
    Employee,
    SyncQueue,
    AuditLog
}

public enum SyncStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public enum OperationType
{
    Create,
    Update,
    Delete
}

public enum PaymentMode
{
    Cash,
    UPI,
    Card,
    Wallet,
    Credit,
    Mixed,
    BankTransfer,
    Cheque
}

public enum BillStatus
{
    Draft,
    Completed,
    Cancelled,
    Returned,
    PartiallyReturned
}

public enum StockMovementType
{
    Purchase,
    Sale,
    Return,
    Adjustment,
    Transfer,
    Opening,
    Damaged,
    Expired
}

public enum LoyaltyTransactionType
{
    Earn,
    Redeem,
    Expire,
    Adjust,
    Bonus,
    Referral
}

public enum UserRole
{
    Admin,
    Manager,
    Cashier,
    Inventory,
    Viewer
}

public enum CustomerType
{
    B2C,
    B2B
}

public enum TaxType
{
    GST,
    IGST,
    VAT,
    None
}
