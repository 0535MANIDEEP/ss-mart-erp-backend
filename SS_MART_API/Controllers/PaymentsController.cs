using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Payment>>> GetPayments(
        [FromQuery] string? search,
        [FromQuery] string? paymentType,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? supplierId,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 50)
    {
        var query = _context.Payments
            .Include(p => p.Customer)
            .Include(p => p.Supplier)
            .Where(p => p.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                p.PaymentNumber.Contains(search) ||
                (p.Description != null && p.Description.Contains(search)) ||
                (p.ReferenceNumber != null && p.ReferenceNumber.Contains(search)));
        }

        if (!string.IsNullOrEmpty(paymentType))
        {
            query = query.Where(p => p.PaymentType == paymentType);
        }

        if (customerId.HasValue)
        {
            query = query.Where(p => p.CustomerId == customerId.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        if (DateTime.TryParse(startDate, out var start))
        {
            query = query.Where(p => p.PaymentDate >= start);
        }

        if (DateTime.TryParse(endDate, out var end))
        {
            query = query.Where(p => p.PaymentDate <= end);
        }

        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(payments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Payment>> GetPayment(Guid id)
    {
        var payment = await _context.Payments
            .Include(p => p.Customer)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (payment == null)
        {
            return NotFound();
        }
        return Ok(payment);
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment([FromBody] Payment payment)
    {
        payment.Id = Guid.NewGuid();
        payment.CreatedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;
        payment.Status = "completed";

        if (string.IsNullOrEmpty(payment.PaymentNumber))
        {
            var count = await _context.Payments.CountAsync(p => p.DeletedAt == null);
            payment.PaymentNumber = $"PAY-{(count + 1).ToString("D4")}";
        }

        // Update customer/supplier balance
        if (payment.PaymentType == "receive" && payment.CustomerId.HasValue)
        {
            var customer = await _context.Customers.FindAsync(payment.CustomerId.Value);
            if (customer != null)
            {
                customer.CurrentBalance -= payment.Amount;
                customer.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (payment.PaymentType == "make" && payment.SupplierId.HasValue)
        {
            var supplier = await _context.Suppliers.FindAsync(payment.SupplierId.Value);
            if (supplier != null)
            {
                supplier.CurrentBalance -= payment.Amount;
                supplier.UpdatedAt = DateTime.UtcNow;
            }
        }

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
    }

    [HttpGet("outstanding")]
    public async Task<ActionResult<object>> GetOutstanding()
    {
        var customersWithBalance = await _context.Customers
            .Where(c => c.CurrentBalance > 0 && c.DeletedAt == null && c.IsActive)
            .Select(c => new
            {
                id = c.Id,
                name = c.Name,
                phone = c.Phone,
                type = "customer",
                outstanding = c.CurrentBalance,
                creditLimit = c.CreditLimit
            })
            .ToListAsync();

        var suppliersWithBalance = await _context.Suppliers
            .Where(s => s.CurrentBalance > 0 && s.DeletedAt == null && s.IsActive)
            .Select(s => new
            {
                id = s.Id,
                name = s.Name,
                phone = s.Phone,
                type = "supplier",
                outstanding = s.CurrentBalance,
                creditLimit = s.CreditLimit
            })
            .ToListAsync();

        var totalReceivable = customersWithBalance.Sum(c => c.outstanding);
        var totalPayable = suppliersWithBalance.Sum(s => s.outstanding);

        return Ok(new
        {
            summary = new
            {
                totalReceivable,
                totalPayable,
                netPosition = totalReceivable - totalPayable
            },
            receivables = customersWithBalance,
            payables = suppliersWithBalance
        });
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetPaymentSummary(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate)
    {
        var query = _context.Payments
            .Where(p => p.DeletedAt == null)
            .AsQueryable();

        if (DateTime.TryParse(startDate, out var start))
        {
            query = query.Where(p => p.PaymentDate >= start);
        }

        if (DateTime.TryParse(endDate, out var end))
        {
            query = query.Where(p => p.PaymentDate <= end);
        }

        var payments = await query.ToListAsync();

        var totalReceived = payments
            .Where(p => p.PaymentType == "receive")
            .Sum(p => p.Amount);

        var totalMade = payments
            .Where(p => p.PaymentType == "make")
            .Sum(p => p.Amount);

        var byMode = payments
            .GroupBy(p => p.PaymentMode)
            .Select(g => new
            {
                paymentMode = g.Key,
                received = g.Where(p => p.PaymentType == "receive").Sum(p => p.Amount),
                made = g.Where(p => p.PaymentType == "make").Sum(p => p.Amount),
                count = g.Count()
            })
            .OrderByDescending(m => m.received + m.made)
            .ToList();

        return Ok(new
        {
            summary = new
            {
                totalReceived,
                totalMade,
                netCashFlow = totalReceived - totalMade,
                totalTransactions = payments.Count
            },
            byPaymentMode = byMode
        });
    }
}
