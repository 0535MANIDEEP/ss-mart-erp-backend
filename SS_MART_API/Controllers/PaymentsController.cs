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

    [HttpGet("aging")]
    public async Task<ActionResult<object>> GetAgingReport()
    {
        var today = DateTime.UtcNow.Date;
        var customers = await _context.Customers
            .Where(c => c.CurrentBalance > 0 && c.DeletedAt == null && c.IsActive)
            .ToListAsync();

        var agingBuckets = new
        {
            current = customers.Where(c => c.CurrentBalance > 0).Select(c => new
            {
                id = c.Id, name = c.Name, phone = c.Phone,
                outstanding = c.CurrentBalance, bucket = "Current (0 days)"
            }).ToList(),
            overdue_1_30 = customers.Where(c => c.CurrentBalance > 0).Select(c => new
            {
                id = c.Id, name = c.Name, phone = c.Phone,
                outstanding = c.CurrentBalance, bucket = "1-30 days overdue"
            }).ToList(),
            overdue_31_60 = new List<object>(),
            overdue_61_90 = new List<object>(),
            overdue_90_plus = new List<object>()
        };

        var bills = await _context.Bills
            .Where(b => b.DueAmount > 0 && b.Status == "completed" && b.DeletedAt == null)
            .ToListAsync();

        var customerBills = bills.GroupBy(b => b.CustomerId);

        var result = new List<object>();
        foreach (var group in customerBills)
        {
            if (!group.Key.HasValue) continue;
            var customer = customers.FirstOrDefault(c => c.Id == group.Key.Value);
            if (customer == null) continue;

            var oldestBill = group.OrderBy(b => b.BillDate).First();
            var daysOverdue = (today - oldestBill.BillDate).Days;
            var totalDue = group.Sum(b => b.DueAmount);

            string bucket;
            if (daysOverdue <= 30) bucket = "0-30 days";
            else if (daysOverdue <= 60) bucket = "31-60 days";
            else if (daysOverdue <= 90) bucket = "61-90 days";
            else bucket = "90+ days";

            result.Add(new
            {
                customerId = customer.Id,
                customerName = customer.Name,
                phone = customer.Phone,
                totalDue,
                oldestBillDate = oldestBill.BillDate,
                daysOverdue,
                bucket,
                billCount = group.Count(),
                bills = group.Select(b => new
                {
                    billId = b.Id,
                    billNumber = b.BillNumber,
                    billDate = b.BillDate,
                    dueAmount = b.DueAmount,
                    daysOverdue = (today - b.BillDate).Days
                }).ToList()
            });
        }

        var grouped = result
            .GroupBy(r => ((dynamic)r).bucket)
            .ToDictionary(g => g.Key, g => new
            {
                count = g.Count(),
                totalAmount = g.Sum(r => ((dynamic)r).totalDue),
                customers = g.ToList()
            });

        return Ok(new
        {
            buckets = grouped,
            totalOutstanding = customers.Sum(c => c.CurrentBalance),
            totalCustomers = customers.Count,
            summary = new
            {
                totalBills = bills.Count,
                totalOverdue = bills.Count(b => (today - b.BillDate).Days > 30),
                avgDaysOverdue = bills.Any() ? bills.Average(b => (today - b.BillDate).Days) : 0
            }
        });
    }

    [HttpGet("reminders")]
    public async Task<ActionResult<IEnumerable<PaymentReminder>>> GetReminders(
        [FromQuery] string? status,
        [FromQuery] string? reminderType,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 50)
    {
        var query = _context.PaymentReminders
            .Include(r => r.Customer)
            .Where(r => r.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrEmpty(reminderType))
            query = query.Where(r => r.ReminderType == reminderType);

        return Ok(await query
            .OrderByDescending(r => r.DaysOverdue)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync());
    }

    [HttpPost("reminders")]
    public async Task<ActionResult<PaymentReminder>> CreateReminder([FromBody] PaymentReminder reminder)
    {
        reminder.Id = Guid.NewGuid();
        reminder.CreatedAt = DateTime.UtcNow;
        reminder.UpdatedAt = DateTime.UtcNow;

        _context.PaymentReminders.Add(reminder);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetReminders), new { id = reminder.Id }, reminder);
    }

    [HttpPost("reminders/{id}/send")]
    public async Task<IActionResult> SendReminder(Guid id)
    {
        var reminder = await _context.PaymentReminders.FindAsync(id);
        if (reminder == null) return NotFound();

        reminder.Status = "sent";
        reminder.SentAt = DateTime.UtcNow;
        reminder.ReminderCount++;
        reminder.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("reminders/bulk-send")]
    public async Task<IActionResult> BulkSendReminders([FromBody] List<Guid> ids)
    {
        var reminders = await _context.PaymentReminders
            .Where(r => ids.Contains(r.Id) && r.Status != "sent")
            .ToListAsync();

        foreach (var reminder in reminders)
        {
            reminder.Status = "sent";
            reminder.SentAt = DateTime.UtcNow;
            reminder.ReminderCount++;
            reminder.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { sent = reminders.Count });
    }

    [HttpGet("reminders/summary")]
    public async Task<ActionResult<object>> GetReminderSummary()
    {
        var reminders = await _context.PaymentReminders
            .Include(r => r.Customer)
            .Where(r => r.DeletedAt == null)
            .ToListAsync();

        return Ok(new
        {
            total = reminders.Count,
            pending = reminders.Count(r => r.Status == "pending"),
            sent = reminders.Count(r => r.Status == "sent"),
            failed = reminders.Count(r => r.Status == "failed"),
            overdue = reminders.Count(r => r.ReminderType == "overdue"),
            dueSoon = reminders.Count(r => r.ReminderType == "due_soon"),
            totalAmountAtRisk = reminders.Where(r => r.ReminderType == "overdue").Sum(r => r.Amount),
            byChannel = reminders.GroupBy(r => r.Channel).Select(g => new
            {
                channel = g.Key,
                count = g.Count(),
                totalAmount = g.Sum(r => r.Amount)
            }).ToList()
        });
    }
}
