using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly AppDbContext _context;

    public SyncController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<object>> Upload([FromBody] SyncUploadRequest request)
    {
        var results = new List<object>();

        foreach (var item in request.Items)
        {
            try
            {
                await ProcessSyncItem(item);
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
                processed = results.Count(r => ((dynamic)r).status == "completed"),
                failed = results.Count(r => ((dynamic)r).status == "failed"),
                results
            }
        });
    }

    [HttpPost("download")]
    public async Task<ActionResult<object>> Download([FromBody] SyncDownloadRequest request)
    {
        var items = new List<object>();

        // Get products updated since last sync
        var products = await _context.Products
            .Where(p => p.UpdatedAt > request.LastSyncTimestamp && p.DeletedAt == null)
            .Take(request.Limit)
            .ToListAsync();

        foreach (var product in products)
        {
            items.Add(new
            {
                entityType = "product",
                entityId = product.Id,
                operation = "update",
                payload = product,
                serverTimestamp = product.UpdatedAt,
                version = product.Version
            });
        }

        // Get customers updated since last sync
        var customers = await _context.Customers
            .Where(c => c.UpdatedAt > request.LastSyncTimestamp && c.DeletedAt == null)
            .Take(request.Limit)
            .ToListAsync();

        foreach (var customer in customers)
        {
            items.Add(new
            {
                entityType = "customer",
                entityId = customer.Id,
                operation = "update",
                payload = customer,
                serverTimestamp = customer.UpdatedAt,
                version = customer.Version
            });
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
        }
    }

    private async Task ProcessProductSync(SyncItem item)
    {
        var existing = await _context.Products.FindAsync(item.EntityId);
        if (existing == null)
        {
            // Create new
            var product = System.Text.Json.JsonSerializer.Deserialize<Product>(item.Payload);
            if (product != null)
            {
                product.Id = item.EntityId;
                product.CreatedAt = DateTime.UtcNow;
                product.UpdatedAt = DateTime.UtcNow;
                _context.Products.Add(product);
            }
        }
        else
        {
            // Update existing
            var updated = System.Text.Json.JsonSerializer.Deserialize<Product>(item.Payload);
            if (updated != null && updated.Version > existing.Version)
            {
                existing.Name = updated.Name;
                existing.SKU = updated.SKU;
                existing.Barcode = updated.Barcode;
                existing.SellingPrice = updated.SellingPrice;
                existing.CurrentStock = updated.CurrentStock;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.Version = updated.Version;
            }
        }
        await _context.SaveChangesAsync();
    }

    private async Task ProcessCustomerSync(SyncItem item)
    {
        var existing = await _context.Customers.FindAsync(item.EntityId);
        if (existing == null)
        {
            var customer = System.Text.Json.JsonSerializer.Deserialize<Customer>(item.Payload);
            if (customer != null)
            {
                customer.Id = item.EntityId;
                customer.CreatedAt = DateTime.UtcNow;
                customer.UpdatedAt = DateTime.UtcNow;
                _context.Customers.Add(customer);
            }
        }
        else
        {
            var updated = System.Text.Json.JsonSerializer.Deserialize<Customer>(item.Payload);
            if (updated != null && updated.Version > existing.Version)
            {
                existing.Name = updated.Name;
                existing.Phone = updated.Phone;
                existing.LoyaltyPoints = updated.LoyaltyPoints;
                existing.CurrentBalance = updated.CurrentBalance;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.Version = updated.Version;
            }
        }
        await _context.SaveChangesAsync();
    }

    private async Task ProcessBillSync(SyncItem item)
    {
        var existing = await _context.Bills.FindAsync(item.EntityId);
        if (existing == null)
        {
            var bill = System.Text.Json.JsonSerializer.Deserialize<Bill>(item.Payload);
            if (bill != null)
            {
                bill.Id = item.EntityId;
                bill.CreatedAt = DateTime.UtcNow;
                bill.UpdatedAt = DateTime.UtcNow;
                _context.Bills.Add(bill);
            }
        }
        await _context.SaveChangesAsync();
    }
}

public class SyncUploadRequest
{
    public List<SyncItem> Items { get; set; } = new();
}

public class SyncDownloadRequest
{
    public DateTime LastSyncTimestamp { get; set; }
    public List<string> EntityTypes { get; set; } = new();
    public int Limit { get; set; } = 100;
}

public class SyncItem
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime ClientTimestamp { get; set; }
}
