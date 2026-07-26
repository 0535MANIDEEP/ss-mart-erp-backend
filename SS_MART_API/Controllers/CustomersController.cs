using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _context;

    public CustomersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var query = _context.Customers
            .Where(c => c.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c =>
                c.Name.Contains(search) ||
                (c.Phone != null && c.Phone.Contains(search)));
        }

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(c => c.Type == type);
        }

        var customers = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetCustomer(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }
        return Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> CreateCustomer(Customer customer)
    {
        customer.Id = Guid.NewGuid();
        customer.CreatedAt = DateTime.UtcNow;
        customer.UpdatedAt = DateTime.UtcNow;
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(Guid id, Customer customer)
    {
        if (id != customer.Id)
        {
            return BadRequest();
        }

        var existingCustomer = await _context.Customers.FindAsync(id);
        if (existingCustomer == null)
        {
            return NotFound();
        }

        existingCustomer.Name = customer.Name;
        existingCustomer.Phone = customer.Phone;
        existingCustomer.Email = customer.Email;
        existingCustomer.Address = customer.Address;
        existingCustomer.City = customer.City;
        existingCustomer.State = customer.State;
        existingCustomer.Pincode = customer.Pincode;
        existingCustomer.GSTIN = customer.GSTIN;
        existingCustomer.Type = customer.Type;
        existingCustomer.CreditLimit = customer.CreditLimit;
        existingCustomer.LoyaltyCardNumber = customer.LoyaltyCardNumber;
        existingCustomer.UpdatedAt = DateTime.UtcNow;
        existingCustomer.Version++;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        customer.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Customer>>> SearchCustomers(
        [FromQuery] string q,
        [FromQuery] int limit = 20)
    {
        var customers = await _context.Customers
            .Where(c => c.DeletedAt == null && (
                c.Name.Contains(q) ||
                (c.Phone != null && c.Phone.Contains(q)) ||
                (c.LoyaltyCardNumber != null && c.LoyaltyCardNumber.Contains(q))))
            .Take(limit)
            .ToListAsync();

        return Ok(customers);
    }

    [HttpGet("{id}/history")]
    public async Task<ActionResult<IEnumerable<object>>> GetCustomerHistory(Guid id)
    {
        var bills = await _context.Bills
            .Where(b => b.CustomerId == id && b.DeletedAt == null)
            .OrderByDescending(b => b.BillDate)
            .Select(b => new
            {
                b.Id,
                b.BillNumber,
                b.BillDate,
                b.TotalAmount,
                b.PaidAmount,
                b.DueAmount,
                b.Status
            })
            .ToListAsync();

        return Ok(bills);
    }

    [HttpGet("{id}/loyalty")]
    public async Task<ActionResult<object>> GetCustomerLoyalty(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        var transactions = await _context.LoyaltyTransactions
            .Where(l => l.CustomerId == id)
            .OrderByDescending(l => l.CreatedAt)
            .Take(10)
            .ToListAsync();

        return Ok(new
        {
            customerId = customer.Id,
            customerName = customer.Name,
            currentBalance = customer.LoyaltyPoints,
            recentTransactions = transactions
        });
    }
}
