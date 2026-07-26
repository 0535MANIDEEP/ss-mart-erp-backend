using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillsController : ControllerBase
{
    private readonly AppDbContext _context;

    public BillsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetBills(
        [FromQuery] Guid? customerId,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var query = _context.Bills
            .Where(b => b.DeletedAt == null)
            .AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(b => b.CustomerId == customerId);
        }

        if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
        {
            query = query.Where(b => b.BillDate >= start);
        }

        if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
        {
            query = query.Where(b => b.BillDate <= end);
        }

        var bills = await query
            .OrderByDescending(b => b.BillDate)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(b => new
            {
                b.Id,
                b.BillNumber,
                b.InvoiceNumber,
                b.CustomerId,
                CustomerName = b.Customer != null ? b.Customer.Name : "Walk-in",
                b.BillDate,
                b.Subtotal,
                b.TaxAmount,
                b.CgstAmount,
                b.SgstAmount,
                b.IgstAmount,
                b.TaxRuleVersion,
                b.DiscountAmount,
                b.RoundOff,
                b.TotalAmount,
                b.PaidAmount,
                b.DueAmount,
                b.PaymentMode,
                b.Status,
                b.IsReturn,
                b.CreatedAt,
                b.Version
            })
            .ToListAsync();

        return Ok(bills);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetBill(Guid id)
    {
        var bill = await _context.Bills
            .Include(b => b.Items)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bill == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            bill.Id,
            bill.BillNumber,
            bill.InvoiceNumber,
            bill.CustomerId,
            CustomerName = bill.Customer?.Name,
            CustomerPhone = bill.Customer?.Phone,
            bill.BillDate,
            bill.Subtotal,
            bill.TaxAmount,
            bill.CgstAmount,
            bill.SgstAmount,
            bill.IgstAmount,
            bill.TaxRuleVersion,
            bill.DiscountAmount,
            bill.RoundOff,
            bill.TotalAmount,
            bill.PaidAmount,
            bill.DueAmount,
            bill.PaymentMode,
            bill.Status,
            bill.IsReturn,
            bill.CreatedAt,
            bill.Version,
            Items = bill.Items.Select(i => new
            {
                i.Id,
                i.ProductId,
                ProductName = i.Product?.Name,
                i.Quantity,
                i.UnitPrice,
                i.DiscountPercent,
                i.DiscountAmount,
                i.TaxAmount,
                i.CgstAmount,
                i.SgstAmount,
                i.IgstAmount,
                i.TaxRuleVersion,
                i.TotalAmount,
                i.BatchNumber
            })
        });
    }

    [HttpPost]
    public async Task<ActionResult<Bill>> CreateBill(Bill bill)
    {
        bill.Id = Guid.NewGuid();
        bill.CreatedAt = DateTime.UtcNow;
        bill.UpdatedAt = DateTime.UtcNow;
        bill.BillNumber = await GenerateBillNumber();

        if (bill.CustomerId.HasValue && bill.PaymentMode == "CREDIT" && bill.DueAmount > 0)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == bill.CustomerId.Value);

            if (customer != null && customer.CreditLimit > 0)
            {
                var newBalance = customer.CurrentBalance + (long)bill.DueAmount;
                if (newBalance > customer.CreditLimit)
                {
                    return BadRequest(new
                    {
                        message = $"Credit limit exceeded. Current balance: {customer.CurrentBalance}, "
                            + $"Credit limit: {customer.CreditLimit}, "
                            + $"This bill due: {bill.DueAmount}"
                    });
                }
            }
        }

        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        // Update stock for each item
        foreach (var item in bill.Items)
        {
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == item.ProductId);

            if (stock != null)
            {
                stock.Quantity -= (int)item.Quantity;
                stock.LastUpdated = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBill), new { id = bill.Id }, bill);
    }

    [HttpPost("{id}/return")]
    public async Task<ActionResult<Bill>> ReturnBill(Guid id, [FromBody] ReturnRequest request)
    {
        var originalBill = await _context.Bills
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (originalBill == null)
        {
            return NotFound();
        }

        var returnBill = new Bill
        {
            Id = Guid.NewGuid(),
            BillNumber = await GenerateBillNumber(),
            CustomerId = originalBill.CustomerId,
            BillDate = DateTime.UtcNow,
            Subtotal = request.Items.Sum(i => (decimal)i.Quantity * i.UnitPrice),
            TaxAmount = 0,
            TotalAmount = request.Items.Sum(i => (decimal)i.Quantity * i.UnitPrice),
            PaymentMode = "CASH",
            Status = "completed",
            IsReturn = true,
            ReferenceBillId = id,
            CreatedBy = originalBill.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = request.Items.Select(i => new BillItem
            {
                Id = Guid.NewGuid(),
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalAmount = (decimal)i.Quantity * i.UnitPrice
            }).ToList()
        };

        _context.Bills.Add(returnBill);

        // Reverse stock
        foreach (var item in returnBill.Items)
        {
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == item.ProductId);

            if (stock != null)
            {
                stock.Quantity += (int)item.Quantity;
                stock.LastUpdated = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBill), new { id = returnBill.Id }, returnBill);
    }

    private async Task<string> GenerateBillNumber()
    {
        var lastBill = await _context.Bills
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync();

        if (lastBill == null)
        {
            return "BILL-0001";
        }

        var lastNumber = int.Parse(lastBill.BillNumber.Split('-')[1]);
        return $"BILL-{(lastNumber + 1).ToString("D4")}";
    }
}

public class ReturnRequest
{
    public List<ReturnItem> Items { get; set; } = new();
    public string? Reason { get; set; }
}

public class ReturnItem
{
    public Guid ProductId { get; set; }
    public double Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
