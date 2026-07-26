using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommissionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CommissionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("rules")]
    public async Task<ActionResult<IEnumerable<CommissionRule>>> GetRules([FromQuery] bool? active)
    {
        var query = _context.CommissionRules
            .Where(r => r.DeletedAt == null)
            .AsQueryable();

        if (active.HasValue)
            query = query.Where(r => r.IsActive == active.Value);

        return Ok(await query.OrderBy(r => r.Name).ToListAsync());
    }

    [HttpPost("rules")]
    public async Task<ActionResult<CommissionRule>> CreateRule([FromBody] CommissionRule rule)
    {
        rule.Id = Guid.NewGuid();
        rule.CreatedAt = DateTime.UtcNow;
        rule.UpdatedAt = DateTime.UtcNow;

        _context.CommissionRules.Add(rule);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRules), new { id = rule.Id }, rule);
    }

    [HttpPut("rules/{id}")]
    public async Task<IActionResult> UpdateRule(Guid id, [FromBody] CommissionRule rule)
    {
        var existing = await _context.CommissionRules.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = rule.Name;
        existing.Type = rule.Type;
        existing.Value = rule.Value;
        existing.MinBillAmount = rule.MinBillAmount;
        existing.MaxBillAmount = rule.MaxBillAmount;
        existing.ProductCategory = rule.ProductCategory;
        existing.IsDefault = rule.IsDefault;
        existing.IsActive = rule.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("entries")]
    public async Task<ActionResult<IEnumerable<CommissionEntry>>> GetEntries(
        [FromQuery] Guid? employeeId,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 50)
    {
        var query = _context.CommissionEntries
            .Include(e => e.Employee)
            .Where(e => e.DeletedAt == null)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(e => e.EmployeeId == employeeId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(e => e.Status == status);

        if (from.HasValue)
            query = query.Where(e => e.SaleDate >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.SaleDate <= to.Value);

        var entries = await query
            .OrderByDescending(e => e.SaleDate)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(entries);
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<object>> CalculateCommission([FromBody] CalculateRequest request)
    {
        var rules = await _context.CommissionRules
            .Where(r => r.IsActive && r.DeletedAt == null)
            .OrderByDescending(r => r.MinBillAmount ?? 0)
            .ToListAsync();

        decimal commission = 0;
        string ruleType = "percentage";
        Guid? matchedRuleId = null;

        foreach (var rule in rules)
        {
            if (rule.MinBillAmount.HasValue && request.SaleAmount < rule.MinBillAmount.Value)
                continue;
            if (rule.MaxBillAmount.HasValue && request.SaleAmount > rule.MaxBillAmount.Value)
                continue;

            switch (rule.Type)
            {
                case "percentage":
                    commission = request.SaleAmount * rule.Value / 100;
                    ruleType = "percentage";
                    matchedRuleId = rule.Id;
                    break;
                case "fixed_per_bill":
                    commission = rule.Value;
                    ruleType = "fixed_per_bill";
                    matchedRuleId = rule.Id;
                    break;
                case "fixed_per_item":
                    commission = rule.Value * request.Quantity;
                    ruleType = "fixed_per_item";
                    matchedRuleId = rule.Id;
                    break;
            }
            break;
        }

        return Ok(new
        {
            commission = Math.Round(commission, 2),
            rate = rules.FirstOrDefault()?.Value ?? 0,
            ruleType,
            matchedRuleId,
            saleAmount = request.SaleAmount
        });
    }

    [HttpPost("entries/{id}/approve")]
    public async Task<IActionResult> ApproveEntry(Guid id)
    {
        var entry = await _context.CommissionEntries.FindAsync(id);
        if (entry == null) return NotFound();

        entry.Status = "approved";
        entry.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("entries/{id}/pay")]
    public async Task<IActionResult> PayEntry(Guid id)
    {
        var entry = await _context.CommissionEntries.FindAsync(id);
        if (entry == null) return NotFound();

        entry.Status = "paid";
        entry.PaidAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("bulk-approve")]
    public async Task<IActionResult> BulkApprove([FromBody] BulkCommissionRequest request)
    {
        var entries = await _context.CommissionEntries
            .Where(e => request.EntryIds.Contains(e.Id) && e.Status == "pending")
            .ToListAsync();

        foreach (var entry in entries)
        {
            entry.Status = "approved";
            entry.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { approved = entries.Count });
    }

    [HttpPost("bulk-pay")]
    public async Task<IActionResult> BulkPay([FromBody] BulkCommissionRequest request)
    {
        var entries = await _context.CommissionEntries
            .Where(e => request.EntryIds.Contains(e.Id) && (e.Status == "approved" || e.Status == "pending"))
            .ToListAsync();

        foreach (var entry in entries)
        {
            entry.Status = "paid";
            entry.PaidAt = DateTime.UtcNow;
            entry.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { paid = entries.Count });
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetSummary(
        [FromQuery] Guid? employeeId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = _context.CommissionEntries
            .Include(e => e.Employee)
            .Where(e => e.DeletedAt == null)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(e => e.EmployeeId == employeeId.Value);

        if (from.HasValue)
            query = query.Where(e => e.SaleDate >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.SaleDate <= to.Value);

        var entries = await query.ToListAsync();

        return Ok(new
        {
            totalCommission = entries.Sum(e => e.CommissionAmount),
            pendingCommission = entries.Where(e => e.Status == "pending").Sum(e => e.CommissionAmount),
            approvedCommission = entries.Where(e => e.Status == "approved").Sum(e => e.CommissionAmount),
            paidCommission = entries.Where(e => e.Status == "paid").Sum(e => e.CommissionAmount),
            totalEntries = entries.Count,
            totalSales = entries.Sum(e => e.SaleAmount),
            byEmployee = entries.GroupBy(e => new { e.EmployeeId, Name = e.Employee.FullName })
                .Select(g => new
                {
                    employeeId = g.Key.EmployeeId,
                    employeeName = g.Key.Name,
                    totalCommission = g.Sum(e => e.CommissionAmount),
                    entryCount = g.Count(),
                    totalSales = g.Sum(e => e.SaleAmount)
                }).ToList()
        });
    }
}

public class CalculateRequest
{
    public decimal SaleAmount { get; set; }
    public int Quantity { get; set; } = 1;
    public Guid? ProductCategoryId { get; set; }
}

public class BulkCommissionRequest
{
    public List<Guid> EntryIds { get; set; } = new();
}
