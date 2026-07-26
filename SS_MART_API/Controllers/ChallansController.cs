using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChallansController : ControllerBase
{
    private readonly AppDbContext _context;

    public ChallansController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeliveryChallan>>> GetChallans(
        [FromQuery] string? status,
        [FromQuery] Guid? customerId,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var query = _context.DeliveryChallans
            .Include(c => c.Customer)
            .Include(c => c.Items)
            .Where(c => c.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(c => c.Status == status);
        }

        if (customerId.HasValue)
        {
            query = query.Where(c => c.CustomerId == customerId.Value);
        }

        var challans = await query
            .OrderByDescending(c => c.ChallanDate)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(challans);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DeliveryChallan>> GetChallan(Guid id)
    {
        var challan = await _context.DeliveryChallans
            .Include(c => c.Customer)
            .Include(c => c.SalesOrder)
            .Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (challan == null)
        {
            return NotFound();
        }
        return Ok(challan);
    }

    [HttpPost]
    public async Task<ActionResult<DeliveryChallan>> CreateChallan(DeliveryChallan challan)
    {
        challan.Id = Guid.NewGuid();
        challan.ChallanNumber = $"DC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
        challan.CreatedAt = DateTime.UtcNow;
        challan.UpdatedAt = DateTime.UtcNow;

        foreach (var item in challan.Items)
        {
            item.Id = Guid.NewGuid();
            item.DeliveryChallanId = challan.Id;
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
        }

        _context.DeliveryChallans.Add(challan);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetChallan), new { id = challan.Id }, challan);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateChallan(Guid id, DeliveryChallan challan)
    {
        if (id != challan.Id)
        {
            return BadRequest();
        }

        var existing = await _context.DeliveryChallans
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (existing == null)
        {
            return NotFound();
        }

        existing.CustomerId = challan.CustomerId;
        existing.SalesOrderId = challan.SalesOrderId;
        existing.ChallanDate = challan.ChallanDate;
        existing.ExpectedDeliveryDate = challan.ExpectedDeliveryDate;
        existing.VehicleNumber = challan.VehicleNumber;
        existing.DriverName = challan.DriverName;
        existing.DriverPhone = challan.DriverPhone;
        existing.Status = challan.Status;
        existing.Notes = challan.Notes;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.Version++;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteChallan(Guid id)
    {
        var challan = await _context.DeliveryChallans.FindAsync(id);
        if (challan == null)
        {
            return NotFound();
        }

        challan.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string status)
    {
        var challan = await _context.DeliveryChallans.FindAsync(id);
        if (challan == null)
        {
            return NotFound();
        }

        challan.Status = status;
        challan.UpdatedAt = DateTime.UtcNow;
        challan.Version++;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
