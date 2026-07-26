using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly AppDbContext _context;

    public SuppliersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var query = _context.Suppliers
            .Where(s => s.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.Name.Contains(search) ||
                (s.Phone != null && s.Phone.Contains(search)) ||
                (s.Email != null && s.Email.Contains(search)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        var suppliers = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(suppliers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Supplier>> GetSupplier(Guid id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
        {
            return NotFound();
        }
        return Ok(supplier);
    }

    [HttpPost]
    public async Task<ActionResult<Supplier>> CreateSupplier(Supplier supplier)
    {
        supplier.Id = Guid.NewGuid();
        supplier.CreatedAt = DateTime.UtcNow;
        supplier.UpdatedAt = DateTime.UtcNow;
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSupplier(Guid id, Supplier supplier)
    {
        if (id != supplier.Id)
        {
            return BadRequest();
        }

        var existing = await _context.Suppliers.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Name = supplier.Name;
        existing.Phone = supplier.Phone;
        existing.Email = supplier.Email;
        existing.Address = supplier.Address;
        existing.City = supplier.City;
        existing.State = supplier.State;
        existing.GSTIN = supplier.GSTIN;
        existing.PAN = supplier.PAN;
        existing.CreditLimit = supplier.CreditLimit;
        existing.CurrentBalance = supplier.CurrentBalance;
        existing.PaymentTerms = supplier.PaymentTerms;
        existing.IsActive = supplier.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.Version++;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSupplier(Guid id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
        {
            return NotFound();
        }

        supplier.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Supplier>>> SearchSuppliers(
        [FromQuery] string q,
        [FromQuery] int limit = 20)
    {
        var suppliers = await _context.Suppliers
            .Where(s => s.DeletedAt == null && (
                s.Name.Contains(q) ||
                (s.Phone != null && s.Phone.Contains(q)) ||
                (s.Email != null && s.Email.Contains(q))))
            .Take(limit)
            .ToListAsync();

        return Ok(suppliers);
    }
}
