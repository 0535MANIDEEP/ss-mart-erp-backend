using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesOrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public SalesOrdersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SalesOrder>>> GetSalesOrders(
        [FromQuery] string? status,
        [FromQuery] Guid? customerId,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var query = _context.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Where(o => o.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (customerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SalesOrder>> GetSalesOrder(Guid id)
    {
        var order = await _context.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }
        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<SalesOrder>> CreateSalesOrder(SalesOrder order)
    {
        order.Id = Guid.NewGuid();
        order.OrderNumber = $"SO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        foreach (var item in order.Items)
        {
            item.Id = Guid.NewGuid();
            item.SalesOrderId = order.Id;
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
        }

        _context.SalesOrders.Add(order);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSalesOrder), new { id = order.Id }, order);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSalesOrder(Guid id, SalesOrder order)
    {
        if (id != order.Id)
        {
            return BadRequest();
        }

        var existing = await _context.SalesOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (existing == null)
        {
            return NotFound();
        }

        existing.CustomerId = order.CustomerId;
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
    public async Task<IActionResult> DeleteSalesOrder(Guid id)
    {
        var order = await _context.SalesOrders.FindAsync(id);
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
        var order = await _context.SalesOrders.FindAsync(id);
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
