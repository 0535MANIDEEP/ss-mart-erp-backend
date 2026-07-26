using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchasesController : ControllerBase
{
    private readonly AppDbContext _context;

    public PurchasesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PurchaseOrder>>> GetPurchaseOrders(
        [FromQuery] string? status,
        [FromQuery] Guid? supplierId,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var query = _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Items)
            .Where(p => p.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        var orders = await query
            .OrderByDescending(p => p.OrderDate)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PurchaseOrder>> GetPurchaseOrder(Guid id)
    {
        var order = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order == null)
        {
            return NotFound();
        }
        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrder>> CreatePurchaseOrder(PurchaseOrder order)
    {
        order.Id = Guid.NewGuid();
        order.OrderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        foreach (var item in order.Items)
        {
            item.Id = Guid.NewGuid();
            item.PurchaseOrderId = order.Id;
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
        }

        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPurchaseOrder), new { id = order.Id }, order);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePurchaseOrder(Guid id, PurchaseOrder order)
    {
        if (id != order.Id)
        {
            return BadRequest();
        }

        var existing = await _context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (existing == null)
        {
            return NotFound();
        }

        existing.SupplierId = order.SupplierId;
        existing.OrderDate = order.OrderDate;
        existing.ExpectedDeliveryDate = order.ExpectedDeliveryDate;
        existing.Subtotal = order.Subtotal;
        existing.TaxAmount = order.TaxAmount;
        existing.DiscountAmount = order.DiscountAmount;
        existing.TotalAmount = order.TotalAmount;
        existing.Status = order.Status;
        existing.Notes = order.Notes;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.Version++;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePurchaseOrder(Guid id)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        order.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string status)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        order.Version++;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
