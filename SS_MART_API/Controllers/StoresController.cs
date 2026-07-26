using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoresController : ControllerBase
{
    private readonly AppDbContext _context;

    public StoresController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Store>>> GetStores([FromQuery] bool? active)
    {
        var query = _context.Stores.Where(s => s.DeletedAt == null).AsQueryable();
        if (active.HasValue)
            query = query.Where(s => s.IsActive == active.Value);

        return Ok(await query.OrderBy(s => s.Name).ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Store>> GetStore(Guid id)
    {
        var store = await _context.Stores.FindAsync(id);
        if (store == null) return NotFound();
        return Ok(store);
    }

    [HttpPost]
    public async Task<ActionResult<Store>> CreateStore([FromBody] Store store)
    {
        store.Id = Guid.NewGuid();
        store.CreatedAt = DateTime.UtcNow;
        store.UpdatedAt = DateTime.UtcNow;

        _context.Stores.Add(store);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetStore), new { id = store.Id }, store);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStore(Guid id, [FromBody] Store store)
    {
        var existing = await _context.Stores.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = store.Name;
        existing.Code = store.Code;
        existing.Address = store.Address;
        existing.City = store.City;
        existing.Phone = store.Phone;
        existing.ManagerName = store.ManagerName;
        existing.IsMain = store.IsMain;
        existing.IsActive = store.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStore(Guid id)
    {
        var store = await _context.Stores.FindAsync(id);
        if (store == null) return NotFound();

        store.DeletedAt = DateTime.UtcNow;
        store.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/stock")]
    public async Task<ActionResult<object>> GetStoreStock(Guid id, [FromQuery] string? locationId)
    {
        var targetLocation = locationId ?? "MAIN";

        var stock = await _context.Stocks
            .Include(s => s.Product)
            .Where(s => s.LocationId == targetLocation && s.DeletedAt == null)
            .ToListAsync();

        var totalProducts = stock.Count;
        var totalQuantity = stock.Sum(s => s.Quantity);
        var totalValue = stock.Sum(s => s.Quantity * (s.Product?.SellingPrice ?? 0));

        return Ok(new
        {
            storeId = id,
            locationId = targetLocation,
            totalProducts,
            totalQuantity,
            totalValue,
            items = stock.Select(s => new
            {
                productId = s.ProductId,
                productName = s.Product?.Name,
                sku = s.Product?.SKU,
                quantity = s.Quantity,
                unitPrice = s.Product?.SellingPrice ?? 0,
                totalValue = s.Quantity * (s.Product?.SellingPrice ?? 0)
            }).ToList()
        });
    }

    [HttpGet("transfers")]
    public async Task<ActionResult<IEnumerable<StockTransferOrder>>> GetTransfers(
        [FromQuery] string? status,
        [FromQuery] string? fromLocation,
        [FromQuery] string? toLocation)
    {
        var query = _context.StockTransferOrders
            .Where(t => t.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);
        if (!string.IsNullOrEmpty(fromLocation))
            query = query.Where(t => t.FromLocationId == fromLocation);
        if (!string.IsNullOrEmpty(toLocation))
            query = query.Where(t => t.ToLocationId == toLocation);

        return Ok(await query.OrderByDescending(t => t.TransferDate).ToListAsync());
    }

    [HttpPost("transfers")]
    public async Task<ActionResult<StockTransferOrder>> CreateTransfer(
        [FromBody] StockTransferOrder transfer)
    {
        transfer.Id = Guid.NewGuid();
        transfer.TransferDate = DateTime.UtcNow;
        transfer.CreatedAt = DateTime.UtcNow;
        transfer.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrEmpty(transfer.TransferNumber))
        {
            var count = await _context.StockTransferOrders.CountAsync(t => t.DeletedAt == null);
            transfer.TransferNumber = $"TRF-{(count + 1).ToString("D4")}";
        }

        _context.StockTransferOrders.Add(transfer);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTransfers), new { id = transfer.Id }, transfer);
    }

    [HttpPost("transfers/{id}/receive")]
    public async Task<IActionResult> ReceiveTransfer(Guid id, [FromBody] ReceiveTransferRequest request)
    {
        var transfer = await _context.StockTransferOrders.FindAsync(id);
        if (transfer == null) return NotFound();
        if (transfer.Status != "pending" && transfer.Status != "in_transit")
            return BadRequest(new { error = "Transfer cannot be received in current status" });

        transfer.Status = "received";
        transfer.ReceivedAt = DateTime.UtcNow;
        transfer.ReceivedBy = request.ReceivedBy;
        transfer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class ReceiveTransferRequest
{
    public Guid? ReceivedBy { get; set; }
}
