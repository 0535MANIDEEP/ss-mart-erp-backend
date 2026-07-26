using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

/// <summary>
/// Central synchronization controller for bidirectional data exchange between
/// the Flutter mobile app and the .NET backend.
///
/// ## Endpoints
/// - POST /api/Sync/upload — push local changes (batch of SyncItems) to server
/// - POST /api/Sync/download — pull server changes since a given timestamp
/// - GET  /api/Sync/status — view pending/failed counts and last sync time
///
/// ## Sync Protocol
/// The mobile app queues every local mutation (create/update/delete) as a
/// SyncQueueItem. When connectivity is available, it uploads them in batches.
/// The server processes each item by upserting into the corresponding entity
/// table using version-based conflict resolution.
///
/// For downloads, the client specifies which entity types it wants and the
/// timestamp of its last successful sync. The server returns all entities
/// modified since that timestamp.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SyncController> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SyncController(AppDbContext context, ILogger<SyncController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Upload a batch of local mutations to the server.
    /// Each SyncItem contains the entity type, ID, operation, and JSON payload.
    /// Server processes via upsert with version-based conflict resolution.
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<object>> Upload([FromBody] SyncUploadRequest request)
    {
        var results = new List<object>();
        var processedCount = 0;
        var failedCount = 0;

        foreach (var item in request.Items)
        {
            try
            {
                await ProcessSyncItem(item);
                processedCount++;
                results.Add(new
                {
                    entityType = item.EntityType,
                    entityId = item.EntityId,
                    status = "completed",
                    serverTimestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex,
                    "Sync upload failed for {EntityType} {EntityId} ({Operation})",
                    item.EntityType, item.EntityId, item.Operation);
                results.Add(new
                {
                    entityType = item.EntityType,
                    entityId = item.EntityId,
                    status = "failed",
                    error = ex.Message
                });
            }
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                processed = processedCount,
                failed = failedCount,
                results
            }
        });
    }

    /// <summary>
    /// Download server-side changes since the given timestamp.
    /// Client specifies which entity types it wants (products, customers, bills, etc.)
    /// and receives all entities modified after LastSyncTimestamp.
    /// </summary>
    [HttpPost("download")]
    public async Task<ActionResult<object>> Download([FromBody] SyncDownloadRequest request)
    {
        var items = new List<object>();
        var entityTypes = request.EntityTypes.Count > 0
            ? request.EntityTypes
            : new List<string> { "product", "customer", "bill", "supplier", "category", "employee" };

        foreach (var entityType in entityTypes)
        {
            var entityItems = await GetEntitiesForDownload(entityType.ToLower(), request.LastSyncTimestamp, request.Limit);
            items.AddRange(entityItems);
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                items,
                serverTimestamp = DateTime.UtcNow,
                totalItems = items.Count
            }
        });
    }

    /// <summary>
    /// Returns sync queue statistics: pending items, failed items, and last sync time.
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<object>> GetStatus()
    {
        var pendingItems = await _context.SyncQueues
            .CountAsync(s => s.Status == "pending");

        var failedItems = await _context.SyncQueues
            .CountAsync(s => s.Status == "failed");

        var lastSync = await _context.SyncQueues
            .Where(s => s.Status == "completed")
            .OrderByDescending(s => s.CompletedAt)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            pendingItems,
            failedItems,
            lastSyncTime = lastSync?.CompletedAt
        });
    }

    /// <summary>
    /// Dispatches a sync item to the appropriate entity-specific processor
    /// based on the entity type string.
    /// </summary>
    private async Task ProcessSyncItem(SyncItem item)
    {
        switch (item.EntityType.ToLower())
        {
            case "product":
                await ProcessProductSync(item);
                break;
            case "customer":
                await ProcessCustomerSync(item);
                break;
            case "bill":
                await ProcessBillSync(item);
                break;
            case "supplier":
                await ProcessSupplierSync(item);
                break;
            case "category":
                await ProcessCategorySync(item);
                break;
            case "employee":
                await ProcessEmployeeSync(item);
                break;
            case "stock":
                await ProcessStockSync(item);
                break;
            case "purchaseorder":
                await ProcessPurchaseOrderSync(item);
                break;
            case "salesorder":
                await ProcessSalesOrderSync(item);
                break;
            case "challan":
            case "deliverychallan":
                await ProcessChallanSync(item);
                break;
            case "ledgerentry":
                await ProcessLedgerEntrySync(item);
                break;
            case "loyaltytransaction":
                await ProcessLoyaltyTransactionSync(item);
                break;
            case "labeltemplate":
                await ProcessLabelTemplateSync(item);
                break;
            case "setting":
                await ProcessSettingSync(item);
                break;
            default:
                _logger.LogWarning("Unknown entity type in sync: {EntityType}", item.EntityType);
                throw new InvalidOperationException($"Unsupported entity type: {item.EntityType}");
        }
    }

    /// <summary>
    /// Upserts a Product entity. Creates if not found; updates fields if
    /// incoming version is higher (optimistic concurrency).
    /// </summary>
    private async Task ProcessProductSync(SyncItem item)
    {
        var existing = await _context.Products.FindAsync(item.EntityId);
        var updated = JsonSerializer.Deserialize<Product>(item.Payload, _jsonOptions);
        if (updated == null) return;

        if (existing == null)
        {
            updated.Id = item.EntityId;
            updated.CreatedAt = DateTime.UtcNow;
            updated.UpdatedAt = DateTime.UtcNow;
            _context.Products.Add(updated);
        }
        else if (updated.Version > existing.Version)
        {
            existing.Name = updated.Name;
            existing.SKU = updated.SKU;
            existing.Barcode = updated.Barcode;
            existing.HSNCode = updated.HSNCode;
            existing.Unit = updated.Unit;
            existing.PackSize = updated.PackSize;
            existing.MRP = updated.MRP;
            existing.SellingPrice = updated.SellingPrice;
            existing.PurchasePrice = updated.PurchasePrice;
            existing.TaxRate = updated.TaxRate;
            existing.TaxType = updated.TaxType;
            existing.CategoryId = updated.CategoryId;
            existing.SupplierId = updated.SupplierId;
            existing.ReorderLevel = updated.ReorderLevel;
            existing.CurrentStock = updated.CurrentStock;
            existing.IsActive = updated.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version = updated.Version;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Upserts a Customer entity with version-based conflict resolution.
    /// </summary>
    private async Task ProcessCustomerSync(SyncItem item)
    {
        var existing = await _context.Customers.FindAsync(item.EntityId);
        var updated = JsonSerializer.Deserialize<Customer>(item.Payload, _jsonOptions);
        if (updated == null) return;

        if (existing == null)
        {
            updated.Id = item.EntityId;
            updated.CreatedAt = DateTime.UtcNow;
            updated.UpdatedAt = DateTime.UtcNow;
            _context.Customers.Add(updated);
        }
        else if (updated.Version > existing.Version)
        {
            existing.Name = updated.Name;
            existing.Phone = updated.Phone;
            existing.Email = updated.Email;
            existing.Address = updated.Address;
            existing.City = updated.City;
            existing.State = updated.State;
            existing.Pincode = updated.Pincode;
            existing.GSTIN = updated.GSTIN;
            existing.Type = updated.Type;
            existing.CreditLimit = updated.CreditLimit;
            existing.CurrentBalance = updated.CurrentBalance;
            existing.LoyaltyPoints = updated.LoyaltyPoints;
            existing.LoyaltyCardNumber = updated.LoyaltyCardNumber;
            existing.IsActive = updated.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version = updated.Version;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a Bill entity. Bills are append-only on the server —
    /// updates and deletes are not supported to preserve audit integrity.
    /// </summary>
    private async Task ProcessBillSync(SyncItem item)
    {
        var existing = await _context.Bills.FindAsync(item.EntityId);
        if (existing != null) return; // Bills are immutable once synced

        var bill = JsonSerializer.Deserialize<Bill>(item.Payload, _jsonOptions);
        if (bill == null) return;

        bill.Id = item.EntityId;
        bill.CreatedAt = DateTime.UtcNow;
        bill.UpdatedAt = DateTime.UtcNow;
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Upserts a Supplier entity with version-based conflict resolution.
    /// </summary>
    private async Task ProcessSupplierSync(SyncItem item)
    {
        var existing = await _context.Suppliers.FindAsync(item.EntityId);
        var updated = JsonSerializer.Deserialize<Supplier>(item.Payload, _jsonOptions);
        if (updated == null) return;

        if (existing == null)
        {
            updated.Id = item.EntityId;
            updated.CreatedAt = DateTime.UtcNow;
            updated.UpdatedAt = DateTime.UtcNow;
            _context.Suppliers.Add(updated);
        }
        else if (updated.Version > existing.Version)
        {
            existing.Name = updated.Name;
            existing.Phone = updated.Phone;
            existing.Email = updated.Email;
            existing.Address = updated.Address;
            existing.City = updated.City;
            existing.State = updated.State;
            existing.GSTIN = updated.GSTIN;
            existing.PAN = updated.PAN;
            existing.CreditLimit = updated.CreditLimit;
            existing.CurrentBalance = updated.CurrentBalance;
            existing.PaymentTerms = updated.PaymentTerms;
            existing.IsActive = updated.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version = updated.Version;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Upserts a Category entity with version-based conflict resolution.
    /// </summary>
    private async Task ProcessCategorySync(SyncItem item)
    {
        var existing = await _context.Categories.FindAsync(item.EntityId);
        var updated = JsonSerializer.Deserialize<Category>(item.Payload, _jsonOptions);
        if (updated == null) return;

        if (existing == null)
        {
            updated.Id = item.EntityId;
            updated.CreatedAt = DateTime.UtcNow;
            updated.UpdatedAt = DateTime.UtcNow;
            _context.Categories.Add(updated);
        }
        else if (updated.Version > existing.Version)
        {
            existing.Name = updated.Name;
            existing.Description = updated.Description;
            existing.Color = updated.Color;
            existing.Icon = updated.Icon;
            existing.SortOrder = updated.SortOrder;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version = updated.Version;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Upserts an Employee entity with version-based conflict resolution.
    /// Note: PasswordHash is synced for credential consistency across devices.
    /// </summary>
    private async Task ProcessEmployeeSync(SyncItem item)
    {
        var existing = await _context.Employees.FindAsync(item.EntityId);
        var updated = JsonSerializer.Deserialize<Employee>(item.Payload, _jsonOptions);
        if (updated == null) return;

        if (existing == null)
        {
            updated.Id = item.EntityId;
            updated.CreatedAt = DateTime.UtcNow;
            updated.UpdatedAt = DateTime.UtcNow;
            _context.Employees.Add(updated);
        }
        else if (updated.Version > existing.Version)
        {
            existing.FullName = updated.FullName;
            existing.Username = updated.Username;
            existing.PasswordHash = updated.PasswordHash;
            existing.Phone = updated.Phone;
            existing.Email = updated.Email;
            existing.Role = updated.Role;
            existing.Pin = updated.Pin;
            existing.StoreId = updated.StoreId;
            existing.IsActive = updated.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version = updated.Version;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Upserts a Stock record. Stock is keyed by ProductId + LocationId
    /// rather than a simple GUID lookup, so we search for existing records.
    /// </summary>
    private async Task ProcessStockSync(SyncItem item)
    {
        var updated = JsonSerializer.Deserialize<Stock>(item.Payload, _jsonOptions);
        if (updated == null) return;

        var existing = await _context.Stocks
            .FirstOrDefaultAsync(s => s.ProductId == updated.ProductId
                && s.LocationId == updated.LocationId);

        if (existing == null)
        {
            updated.Id = item.EntityId;
            updated.CreatedAt = DateTime.UtcNow;
            updated.UpdatedAt = DateTime.UtcNow;
            _context.Stocks.Add(updated);
        }
        else if (updated.Version > existing.Version)
        {
            existing.Quantity = updated.Quantity;
            existing.ReservedQuantity = updated.ReservedQuantity;
            existing.BatchNumber = updated.BatchNumber;
            existing.ExpiryDate = updated.ExpiryDate;
            existing.LastUpdated = updated.LastUpdated;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version = updated.Version;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Upserts a PurchaseOrder with version-based conflict resolution.
    /// </summary>
    private async Task ProcessPurchaseOrderSync(SyncItem item)
    {
        var existing = await _context.PurchaseOrders.FindAsync(item.EntityId);
        var updated = JsonSerializer.Deserialize<PurchaseOrder>(item.Payload, _jsonOptions);
        if (updated == null) return;

        if (existing == null)
        {
            updated.Id = item.EntityId;
            updated.CreatedAt = DateTime.UtcNow;
            updated.UpdatedAt = DateTime.UtcNow;
            _context.PurchaseOrders.Add(updated);
        }
        else if (updated.Version > existing.Version)
        {
            existing.OrderNumber = updated.OrderNumber;
            existing.SupplierId = updated.SupplierId;
            existing.OrderDate = updated.OrderDate;
            existing.ExpectedDeliveryDate = updated.ExpectedDeliveryDate;
            existing.Subtotal = updated.Subtotal;
            existing.TaxAmount = updated.TaxAmount;
            existing.DiscountAmount = updated.DiscountAmount;
            existing.TotalAmount = updated.TotalAmount;
            existing.Status = updated.Status;
            existing.Notes = updated.Notes;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version = updated.Version;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Upserts a SalesOrder with version-based conflict resolution.
    /// </summary>
    private async Task ProcessSalesOrderSync(SyncItem item)
    {
        var existing = await _context.SalesOrders.FindAsync(item.EntityId);
        var updated = JsonSerializer.Deserialize<SalesOrder>(item.Payload, _jsonOptions);
        if (updated == null) return;

        if (existing == null)
        {
            updated.Id = item.EntityId;
            updated.CreatedAt = DateTime.UtcNow;
            updated.UpdatedAt = DateTime.UtcNow;
            _context.SalesOrders.Add(updated);
        }
        else if (updated.Version > existing.Version)
        {
            existing.OrderNumber = updated.OrderNumber;
            existing.CustomerId = updated.CustomerId;
            existing.OrderDate = updated.OrderDate;
            existing.ExpectedDeliveryDate = updated.ExpectedDeliveryDate;
            existing.Subtotal = updated.Subtotal;
            existing.TaxAmount = updated.TaxAmount;
            existing.DiscountAmount = updated.DiscountAmount;
            existing.TotalAmount = updated.TotalAmount;
            existing.Status = updated.Status;
            existing.Notes = updated.Notes;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version = updated.Version;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Upserts a DeliveryChallan with version-based conflict resolution.
    /// </summary>
    private async Task ProcessChallanSync(SyncItem item)
    {
        var existing = await _context.DeliveryChallans.FindAsync(item.EntityId);
        var updated = JsonSerializer.Deserialize<DeliveryChallan>(item.Payload, _jsonOptions);
        if (updated == null) return;

        if (existing == null)
        {
            updated.Id = item.EntityId;
            updated.CreatedAt = DateTime.UtcNow;
            updated.UpdatedAt = DateTime.UtcNow;
            _context.DeliveryChallans.Add(updated);
        }
        else if (updated.Version > existing.Version)
        {
            existing.ChallanNumber = updated.ChallanNumber;
            existing.CustomerId = updated.CustomerId;
            existing.SalesOrderId = updated.SalesOrderId;
            existing.ChallanDate = updated.ChallanDate;
            existing.ExpectedDeliveryDate = updated.ExpectedDeliveryDate;
            existing.VehicleNumber = updated.VehicleNumber;
            existing.DriverName = updated.DriverName;
            existing.DriverPhone = updated.DriverPhone;
            existing.Status = updated.Status;
            existing.Notes = updated.Notes;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version = updated.Version;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Upserts a LedgerEntry. Ledger entries are append-only —
    /// once posted they cannot be modified to preserve accounting integrity.
    /// </summary>
    private async Task ProcessLedgerEntrySync(SyncItem item)
    {
        var existing = await _context.LedgerEntries.FindAsync(item.EntityId);
        if (existing != null) return; // Ledger entries are immutable once posted

        var entry = JsonSerializer.Deserialize<LedgerEntry>(item.Payload, _jsonOptions);
        if (entry == null) return;

        entry.Id = item.EntityId;
        entry.CreatedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;
        _context.LedgerEntries.Add(entry);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Upserts a LoyaltyTransaction. Transactions are append-only —
    /// they represent point accrual or redemption events.
    /// </summary>
    private async Task ProcessLoyaltyTransactionSync(SyncItem item)
    {
        var existing = await _context.LoyaltyTransactions.FindAsync(item.EntityId);
        if (existing != null) return; // Loyalty transactions are immutable

        var txn = JsonSerializer.Deserialize<LoyaltyTransaction>(item.Payload, _jsonOptions);
        if (txn == null) return;

        txn.Id = item.EntityId;
        txn.CreatedAt = DateTime.UtcNow;
        txn.UpdatedAt = DateTime.UtcNow;
        _context.LoyaltyTransactions.Add(txn);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Upserts a LabelTemplate with version-based conflict resolution.
    /// </summary>
    private async Task ProcessLabelTemplateSync(SyncItem item)
    {
        var existing = await _context.LabelTemplates.FindAsync(item.EntityId);
        var updated = JsonSerializer.Deserialize<LabelTemplate>(item.Payload, _jsonOptions);
        if (updated == null) return;

        if (existing == null)
        {
            updated.Id = item.EntityId;
            updated.CreatedAt = DateTime.UtcNow;
            updated.UpdatedAt = DateTime.UtcNow;
            _context.LabelTemplates.Add(updated);
        }
        else if (updated.Version > existing.Version)
        {
            existing.Name = updated.Name;
            existing.Type = updated.Type;
            existing.Width = updated.Width;
            existing.Height = updated.Height;
            existing.Layout = updated.Layout;
            existing.IsDefault = updated.IsDefault;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version = updated.Version;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Upserts a Setting with version-based conflict resolution.
    /// Settings are keyed by their Key property within a Category.
    /// </summary>
    private async Task ProcessSettingSync(SyncItem item)
    {
        var updated = JsonSerializer.Deserialize<Setting>(item.Payload, _jsonOptions);
        if (updated == null) return;

        var existing = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == updated.Key && s.Category == updated.Category);

        if (existing == null)
        {
            updated.Id = item.EntityId;
            updated.CreatedAt = DateTime.UtcNow;
            updated.UpdatedAt = DateTime.UtcNow;
            _context.Settings.Add(updated);
        }
        else if (updated.Version > existing.Version)
        {
            existing.Value = updated.Value;
            existing.Description = updated.Description;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version = updated.Version;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Queries the appropriate entity table and returns items modified since
    /// the given timestamp, formatted as SyncDownloadItem payloads.
    /// </summary>
    private async Task<List<object>> GetEntitiesForDownload(string entityType, DateTime since, int limit)
    {
        return entityType switch
        {
            "product" => (await _context.Products
                .Where(p => p.UpdatedAt > since && p.DeletedAt == null)
                .Take(limit).ToListAsync())
                .Select(p => (object)new
                {
                    entityType = "product",
                    entityId = p.Id,
                    operation = "update",
                    payload = p,
                    serverTimestamp = p.UpdatedAt,
                    version = p.Version
                }).ToList(),

            "customer" => (await _context.Customers
                .Where(c => c.UpdatedAt > since && c.DeletedAt == null)
                .Take(limit).ToListAsync())
                .Select(c => (object)new
                {
                    entityType = "customer",
                    entityId = c.Id,
                    operation = "update",
                    payload = c,
                    serverTimestamp = c.UpdatedAt,
                    version = c.Version
                }).ToList(),

            "bill" => (await _context.Bills
                .Include(b => b.Items)
                .Where(b => b.UpdatedAt > since && b.DeletedAt == null)
                .Take(limit).ToListAsync())
                .Select(b => (object)new
                {
                    entityType = "bill",
                    entityId = b.Id,
                    operation = "update",
                    payload = b,
                    serverTimestamp = b.UpdatedAt,
                    version = b.Version
                }).ToList(),

            "supplier" => (await _context.Suppliers
                .Where(s => s.UpdatedAt > since && s.DeletedAt == null)
                .Take(limit).ToListAsync())
                .Select(s => (object)new
                {
                    entityType = "supplier",
                    entityId = s.Id,
                    operation = "update",
                    payload = s,
                    serverTimestamp = s.UpdatedAt,
                    version = s.Version
                }).ToList(),

            "category" => (await _context.Categories
                .Where(c => c.UpdatedAt > since && c.DeletedAt == null)
                .Take(limit).ToListAsync())
                .Select(c => (object)new
                {
                    entityType = "category",
                    entityId = c.Id,
                    operation = "update",
                    payload = c,
                    serverTimestamp = c.UpdatedAt,
                    version = c.Version
                }).ToList(),

            "employee" => (await _context.Employees
                .Where(e => e.UpdatedAt > since && e.DeletedAt == null)
                .Take(limit).ToListAsync())
                .Select(e => (object)new
                {
                    entityType = "employee",
                    entityId = e.Id,
                    operation = "update",
                    payload = e,
                    serverTimestamp = e.UpdatedAt,
                    version = e.Version
                }).ToList(),

            "stock" => (await _context.Stocks
                .Where(s => s.UpdatedAt > since && s.DeletedAt == null)
                .Take(limit).ToListAsync())
                .Select(s => (object)new
                {
                    entityType = "stock",
                    entityId = s.Id,
                    operation = "update",
                    payload = s,
                    serverTimestamp = s.UpdatedAt,
                    version = s.Version
                }).ToList(),

            "purchaseorder" => (await _context.PurchaseOrders
                .Where(p => p.UpdatedAt > since && p.DeletedAt == null)
                .Take(limit).ToListAsync())
                .Select(p => (object)new
                {
                    entityType = "purchaseorder",
                    entityId = p.Id,
                    operation = "update",
                    payload = p,
                    serverTimestamp = p.UpdatedAt,
                    version = p.Version
                }).ToList(),

            "salesorder" => (await _context.SalesOrders
                .Where(s => s.UpdatedAt > since && s.DeletedAt == null)
                .Take(limit).ToListAsync())
                .Select(s => (object)new
                {
                    entityType = "salesorder",
                    entityId = s.Id,
                    operation = "update",
                    payload = s,
                    serverTimestamp = s.UpdatedAt,
                    version = s.Version
                }).ToList(),

            "challan" or "deliverychallan" => (await _context.DeliveryChallans
                .Where(d => d.UpdatedAt > since && d.DeletedAt == null)
                .Take(limit).ToListAsync())
                .Select(d => (object)new
                {
                    entityType = "challan",
                    entityId = d.Id,
                    operation = "update",
                    payload = d,
                    serverTimestamp = d.UpdatedAt,
                    version = d.Version
                }).ToList(),

            "ledgerentry" => (await _context.LedgerEntries
                .Where(l => l.UpdatedAt > since && l.DeletedAt == null)
                .Take(limit).ToListAsync())
                .Select(l => (object)new
                {
                    entityType = "ledgerentry",
                    entityId = l.Id,
                    operation = "update",
                    payload = l,
                    serverTimestamp = l.UpdatedAt,
                    version = l.Version
                }).ToList(),

            _ => new List<object>()
        };
    }
}

/// <summary>
/// Request model for batch upload from the mobile client.
/// Contains a list of SyncItem objects representing local mutations.
/// </summary>
public class SyncUploadRequest
{
    public List<SyncItem> Items { get; set; } = new();
}

/// <summary>
/// Request model for batch download. Client specifies which entity types
/// it wants and the timestamp of its last successful sync.
/// </summary>
public class SyncDownloadRequest
{
    public DateTime LastSyncTimestamp { get; set; }
    public List<string> EntityTypes { get; set; } = new();
    public int Limit { get; set; } = 100;
}

/// <summary>
/// Represents a single sync mutation: the entity type, its GUID,
/// the operation (create/update/delete), the JSON payload, and a
/// client timestamp for ordering.
/// </summary>
public class SyncItem
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime ClientTimestamp { get; set; }
}
