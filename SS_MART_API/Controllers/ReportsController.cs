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

    [HttpGet("hsn-summary")]
    public async Task<ActionResult<object>> GetHsnSummaryReport(
        [FromQuery] string startDate,
        [FromQuery] string endDate)
    {
        if (!DateTime.TryParse(startDate, out var start) ||
            !DateTime.TryParse(endDate, out var end))
        {
            return BadRequest(new { message = "Invalid date format" });
        }

        var billItems = await _context.BillItems
            .Include(bi => bi.Bill)
            .Include(bi => bi.Product)
            .Where(bi => bi.Bill.BillDate >= start
                && bi.Bill.BillDate <= end
                && !bi.Bill.IsReturn
                && bi.Bill.DeletedAt == null)
            .ToListAsync();

        var hsnSummary = billItems
            .GroupBy(bi => new
            {
                HsnCode = bi.Product?.HSNCode ?? "UNKNOWN",
                TaxRate = bi.Product?.TaxRate ?? 0
            })
            .Select(g => new
            {
                hsnCode = g.Key.HsnCode,
                taxRate = g.Key.TaxRate,
                totalQuantity = g.Sum(bi => bi.Quantity),
                taxableAmount = g.Sum(bi => bi.UnitPrice * (decimal)bi.Quantity),
                cgstAmount = g.Sum(bi => bi.CgstAmount),
                sgstAmount = g.Sum(bi => bi.SgstAmount),
                igstAmount = g.Sum(bi => bi.IgstAmount),
                totalTax = g.Sum(bi => bi.TaxAmount),
                totalAmount = g.Sum(bi => bi.TotalAmount),
                itemCount = g.Count()
            })
            .OrderBy(h => h.hsnCode)
            .ThenBy(h => h.taxRate)
            .ToList();

        var summary = new
        {
            totalHsnCodes = hsnSummary.Count(),
            totalTaxableAmount = hsnSummary.Sum(h => h.taxableAmount),
            totalCgst = hsnSummary.Sum(h => h.cgstAmount),
            totalSgst = hsnSummary.Sum(h => h.sgstAmount),
            totalIgst = hsnSummary.Sum(h => h.igstAmount),
            totalTax = hsnSummary.Sum(h => h.totalTax),
            totalItems = hsnSummary.Sum(h => h.itemCount)
        };

        return Ok(new
        {
            summary,
            hsnDetails = hsnSummary
        });
    }

    [HttpGet("profit-loss")]
    public async Task<ActionResult<object>> GetProfitLossReport(
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

        var purchases = await _context.PurchaseOrders
            .Where(p => p.OrderDate >= start && p.OrderDate <= end && p.DeletedAt == null)
            .ToListAsync();

        var totalRevenue = bills.Sum(b => b.TotalAmount);
        var totalCost = purchases.Sum(p => p.TotalAmount);
        var grossProfit = totalRevenue - totalCost;
        var taxCollected = bills.Sum(b => b.TaxAmount);
        var taxPaidOnPurchases = purchases.Sum(p => p.TaxAmount);

        var expenses = await _context.Expenses
            .Where(e => e.ExpenseDate >= start && e.ExpenseDate <= end && e.DeletedAt == null)
            .ToListAsync();

        var totalExpenses = expenses.Sum(e => e.Amount);
        var netProfit = grossProfit - totalExpenses;

        return Ok(new
        {
            summary = new
            {
                totalRevenue,
                totalCost,
                grossProfit,
                totalExpenses,
                netProfit,
                taxCollected,
                taxPaidOnPurchases,
                netTaxLiability = taxCollected - taxPaidOnPurchases
            },
            dailyBreakdown = bills
                .GroupBy(b => b.BillDate.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    revenue = g.Sum(b => b.TotalAmount),
                    bills = g.Count()
                })
                .OrderBy(d => d.date)
                .ToList()
        });
    }

    [HttpGet("gstr1")]
    public async Task<ActionResult<object>> GetGSTR1Report(
        [FromQuery] string startDate,
        [FromQuery] string endDate)
    {
        if (!DateTime.TryParse(startDate, out var start) ||
            !DateTime.TryParse(endDate, out var end))
        {
            return BadRequest(new { message = "Invalid date format" });
        }

        var bills = await _context.Bills
            .Include(b => b.Items)
            .ThenInclude(i => i.Product)
            .Where(b => b.BillDate >= start && b.BillDate <= end && !b.IsReturn && b.DeletedAt == null)
            .ToListAsync();

        var invoiceDetails = bills.Select(b => new
        {
            invoiceNumber = b.InvoiceNumber ?? b.BillNumber,
            invoiceDate = b.BillDate.ToString("yyyy-MM-dd"),
            invoiceValue = b.TotalAmount,
            taxableValue = b.Subtotal,
            cgst = b.CgstAmount,
            sgst = b.SgstAmount,
            igst = b.IgstAmount,
            placeOfSupply = "27-Maharashtra",
            reverseCharge = false,
            items = b.Items.Select(i => new
            {
                hsnCode = i.Product?.HSNCode ?? "UNKNOWN",
                description = i.Product?.Name ?? "Unknown",
                quantity = i.Quantity,
                unitPrice = i.UnitPrice,
                taxableValue = i.UnitPrice * (decimal)i.Quantity,
                cgst = i.CgstAmount,
                sgst = i.SgstAmount,
                igst = i.IgstAmount
            }).ToList()
        }).ToList();

        var hsnSummary = bills
            .SelectMany(b => b.Items)
            .GroupBy(i => new { Hsn = i.Product?.HSNCode ?? "UNKNOWN", Rate = i.Product?.TaxRate ?? 0 })
            .Select(g => new
            {
                hsnCode = g.Key.Hsn,
                uqc = "NOS",
                totalQuantity = g.Sum(i => i.Quantity),
                totalValue = g.Sum(i => i.UnitPrice * (decimal)i.Quantity),
                taxableValue = g.Sum(i => i.UnitPrice * (decimal)i.Quantity),
                cgst = g.Sum(i => i.CgstAmount),
                sgst = g.Sum(i => i.SgstAmount),
                igst = g.Sum(i => i.IgstAmount)
            })
            .OrderBy(h => h.hsnCode)
            .ToList();

        return Ok(new
        {
            period = new { from = start.ToString("yyyy-MM-dd"), to = end.ToString("yyyy-MM-dd") },
            summary = new
            {
                totalInvoices = bills.Count,
                totalTaxableValue = bills.Sum(b => b.Subtotal),
                totalCgst = bills.Sum(b => b.CgstAmount),
                totalSgst = bills.Sum(b => b.SgstAmount),
                totalIgst = bills.Sum(b => b.IgstAmount),
                totalInvoiceValue = bills.Sum(b => b.TotalAmount)
            },
            invoiceDetails,
            hsnSummary
        });
    }

    [HttpGet("gstr3b")]
    public async Task<ActionResult<object>> GetGSTR3BReport(
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

        var purchases = await _context.PurchaseOrders
            .Where(p => p.OrderDate >= start && p.OrderDate <= end && p.DeletedAt == null)
            .ToListAsync();

        var totalTaxableSales = bills.Sum(b => b.Subtotal);
        var totalCgstOnSales = bills.Sum(b => b.CgstAmount);
        var totalSgstOnSales = bills.Sum(b => b.SgstAmount);
        var totalIgstOnSales = bills.Sum(b => b.IgstAmount);

        var totalTaxablePurchases = purchases.Sum(p => p.Subtotal);
        var totalCgstOnPurchases = purchases.Sum(p => p.TaxAmount / 2);
        var totalSgstOnPurchases = purchases.Sum(p => p.TaxAmount / 2);

        return Ok(new
        {
            period = new { from = start.ToString("yyyy-MM-dd"), to = end.ToString("yyyy-MM-dd") },
            outwardSupplies = new
            {
                taxableValue = totalTaxableSales,
                cgst = totalCgstOnSales,
                sgst = totalSgstOnSales,
                igst = totalIgstOnSales,
                totalTax = totalCgstOnSales + totalSgstOnSales + totalIgstOnSales
            },
            inwardSupplies = new
            {
                taxableValue = totalTaxablePurchases,
                cgst = totalCgstOnPurchases,
                sgst = totalSgstOnPurchases,
                totalItc = totalCgstOnPurchases + totalSgstOnPurchases
            },
            taxPayable = new
            {
                cgst = totalCgstOnSales - totalCgstOnPurchases,
                sgst = totalSgstOnSales - totalSgstOnPurchases,
                igst = totalIgstOnSales,
                total = (totalCgstOnSales - totalCgstOnPurchases) +
                        (totalSgstOnSales - totalSgstOnPurchases) +
                        totalIgstOnSales
            }
        });
    }
}
