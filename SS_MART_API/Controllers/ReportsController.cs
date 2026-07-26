using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("sales")]
    public async Task<ActionResult<object>> GetSalesReport(
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        [FromQuery] string? groupBy)
    {
        if (!DateTime.TryParse(startDate, out var start) ||
            !DateTime.TryParse(endDate, out var end))
        {
            return BadRequest(new { message = "Invalid date format" });
        }

        var bills = await _context.Bills
            .Where(b => b.BillDate >= start && b.BillDate <= end && !b.IsReturn && b.DeletedAt == null)
            .ToListAsync();

        var summary = new
        {
            totalSales = bills.Sum(b => b.TotalAmount),
            totalBills = bills.Count,
            averageBillValue = bills.Any() ? bills.Average(b => b.TotalAmount) : 0,
            totalTax = bills.Sum(b => b.TaxAmount),
            totalDiscount = bills.Sum(b => b.DiscountAmount)
        };

        var dailyBreakdown = bills
            .GroupBy(b => b.BillDate.Date)
            .Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                sales = g.Sum(b => b.TotalAmount),
                bills = g.Count()
            })
            .OrderBy(d => d.date)
            .ToList();

        return Ok(new
        {
            summary,
            dailyBreakdown
        });
    }

    [HttpGet("inventory")]
    public async Task<ActionResult<object>> GetInventoryReport([FromQuery] string? locationId)
    {
        var stocks = await _context.Stocks
            .Include(s => s.Product)
            .Where(s => s.DeletedAt == null)
            .ToListAsync();

        var summary = new
        {
            totalProducts = stocks.Count,
            totalStockValue = stocks.Sum(s => s.Quantity * s.Product.SellingPrice),
            lowStockProducts = stocks.Count(s => s.Quantity <= s.Product.ReorderLevel),
            outOfStockProducts = stocks.Count(s => s.Quantity <= 0)
        };

        var categoryBreakdown = stocks
            .GroupBy(s => s.Product.CategoryId)
            .Select(g => new
            {
                categoryId = g.Key,
                productCount = g.Count(),
                stockValue = g.Sum(s => s.Quantity * s.Product.SellingPrice)
            })
            .ToList();

        return Ok(new
        {
            summary,
            categoryBreakdown
        });
    }

    [HttpGet("tax")]
    public async Task<ActionResult<object>> GetTaxReport(
        [FromQuery] string startDate,
        [FromQuery] string endDate)
    {
        if (!DateTime.TryParse(startDate, out var start) ||
            !DateTime.TryParse(endDate, out var end))
        {
            return BadRequest(new { message = "Invalid date format" });
        }

        var bills = await _context.Bills
            .Where(b => b.BillDate >= start && b.BillDate <= end && !b.IsReturn && b.DeletedAt == null)
            .ToListAsync();

        var summary = new
        {
            totalTaxableSales = bills.Sum(b => b.Subtotal),
            totalTaxCollected = bills.Sum(b => b.TaxAmount),
            netTaxPayable = bills.Sum(b => b.TaxAmount)
        };

        return Ok(new
        {
            summary
        });
    }

    [HttpGet("customers")]
    public async Task<ActionResult<object>> GetCustomerReport()
    {
        var customers = await _context.Customers
            .Where(c => c.DeletedAt == null)
            .ToListAsync();

        var summary = new
        {
            totalCustomers = customers.Count,
            b2bCustomers = customers.Count(c => c.Type == "B2B"),
            b2cCustomers = customers.Count(c => c.Type == "B2C"),
            totalOutstanding = customers.Sum(c => c.CurrentBalance),
            totalLoyaltyPoints = customers.Sum(c => c.LoyaltyPoints)
        };

        return Ok(new
        {
            summary
        });
    }

    [HttpGet("loyalty")]
    public async Task<ActionResult<object>> GetLoyaltyReport()
    {
        var transactions = await _context.LoyaltyTransactions
            .ToListAsync();

        var summary = new
        {
            totalPointsIssued = transactions
                .Where(l => l.TransactionType == "earn")
                .Sum(l => l.Points),
            totalPointsRedeemed = transactions
                .Where(l => l.TransactionType == "redeem")
                .Sum(l => l.Points),
            totalTransactions = transactions.Count
        };

        return Ok(new
        {
            summary
        });
    }
}
