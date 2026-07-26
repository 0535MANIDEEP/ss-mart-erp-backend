using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpiryController : ControllerBase
{
    private readonly AppDbContext _context;

    public ExpiryController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("batches")]
    public async Task<ActionResult<IEnumerable<ExpiryBatch>>> GetBatches(
        [FromQuery] Guid? productId,
        [FromQuery] string? status,
        [FromQuery] int? expiringWithinDays,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 50)
    {
        var query = _context.ExpiryBatches
            .Include(b => b.Product)
            .Where(b => b.DeletedAt == null)
            .AsQueryable();

        if (productId.HasValue)
            query = query.Where(b => b.ProductId == productId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(b => b.Status == status);

        if (expiringWithinDays.HasValue)
        {
            var cutoff = DateTime.UtcNow.Date.AddDays(expiringWithinDays.Value);
            query = query.Where(b => b.ExpiryDate <= cutoff && b.Status == "active");
        }

        var batches = await query
            .OrderBy(b => b.ExpiryDate)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(batches);
    }

    [HttpPost("batches")]
    public async Task<ActionResult<ExpiryBatch>> AddBatch([FromBody] ExpiryBatch batch)
    {
        batch.Id = Guid.NewGuid();
        batch.CreatedAt = DateTime.UtcNow;
        batch.UpdatedAt = DateTime.UtcNow;

        _context.ExpiryBatches.Add(batch);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBatches), new { productId = batch.ProductId }, batch);
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<object>> GetExpiryAlerts([FromQuery] int withinDays = 30)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(withinDays);
        var today = DateTime.UtcNow.Date;

        var batches = await _context.ExpiryBatches
            .Include(b => b.Product)
            .Where(b => b.Status == "active" && b.DeletedAt == null)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync();

        var expired = batches.Where(b => b.ExpiryDate.Date <= today).ToList();
        var critical = batches.Where(b => b.ExpiryDate.Date > today && b.ExpiryDate.Date <= cutoff.AddDays(7)).ToList();
        var warning = batches.Where(b => b.ExpiryDate.Date > cutoff.AddDays(7) && b.ExpiryDate.Date <= cutoff).ToList();

        return Ok(new
        {
            withinDays,
            expired = expired.Select(b => new
            {
                b.Id,
                productName = b.Product.Name,
                b.BatchNumber,
                b.Quantity,
                b.ExpiryDate,
                daysExpired = (today - b.ExpiryDate.Date).Days,
                status = "expired"
            }),
            critical = critical.Select(b => new
            {
                b.Id,
                productName = b.Product.Name,
                b.BatchNumber,
                b.Quantity,
                b.ExpiryDate,
                daysUntilExpiry = (b.ExpiryDate.Date - today).Days,
                status = "critical"
            }),
            warning = warning.Select(b => new
            {
                b.Id,
                productName = b.Product.Name,
                b.BatchNumber,
                b.Quantity,
                b.ExpiryDate,
                daysUntilExpiry = (b.ExpiryDate.Date - today).Days,
                status = "warning"
            }),
            summary = new
            {
                totalActive = batches.Count,
                totalExpired = expired.Count,
                totalCritical = critical.Count,
                totalWarning = warning.Count,
                totalQuantityAtRisk = expired.Sum(b => b.Quantity) + critical.Sum(b => b.Quantity) + warning.Sum(b => b.Quantity),
                totalValueAtRisk = expired.Sum(b => b.Quantity * (b.Product?.SellingPrice ?? 0)) + critical.Sum(b => b.Quantity * (b.Product?.SellingPrice ?? 0)) + warning.Sum(b => b.Quantity * (b.Product?.SellingPrice ?? 0))
            }
        });
    }

    [HttpPost("batches/{id}/writeoff")]
    public async Task<IActionResult> WriteOff(Guid id, [FromBody] WriteOffRequest request)
    {
        var batch = await _context.ExpiryBatches
            .Include(b => b.Product)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (batch == null) return NotFound();
        if (batch.Status != "active" && batch.Status != "expired")
            return BadRequest(new { error = "Only active or expired batches can be written off" });

        batch.Status = "written_off";
        batch.WriteOffAmount = request.Amount;
        batch.WriteOffReason = request.Reason;
        batch.WrittenOffAt = DateTime.UtcNow;
        batch.UpdatedAt = DateTime.UtcNow;

        var stock = await _context.Stocks.FirstOrDefaultAsync(s =>
            s.ProductId == batch.ProductId && s.BatchNumber == batch.BatchNumber && s.DeletedAt == null);

        if (stock != null)
        {
            stock.Quantity -= batch.Quantity;
            if (stock.Quantity < 0) stock.Quantity = 0;
            stock.LastUpdated = DateTime.UtcNow;
            stock.UpdatedAt = DateTime.UtcNow;
        }

        _context.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            ProductId = batch.ProductId,
            MovementType = "expiry_writeoff",
            Quantity = -batch.Quantity,
            ReferenceType = "expiry_batch",
            ReferenceId = batch.Id,
            Notes = $"Written off {batch.Quantity} units — Batch: {batch.BatchNumber}. Reason: {request.Reason}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetExpirySummary()
    {
        var today = DateTime.UtcNow.Date;

        var batches = await _context.ExpiryBatches
            .Include(b => b.Product)
            .Where(b => b.DeletedAt == null && b.Status == "active")
            .ToListAsync();

        var grouped = batches
            .GroupBy(b =>
            {
                var days = (b.ExpiryDate.Date - today).Days;
                if (days <= 0) return "Expired";
                if (days <= 7) return "1-7 days";
                if (days <= 30) return "8-30 days";
                if (days <= 90) return "31-90 days";
                return "90+ days";
            })
            .ToDictionary(g => g.Key, g => new
            {
                count = g.Count(),
                totalQuantity = g.Sum(b => b.Quantity),
                totalValue = g.Sum(b => b.Quantity * (b.Product?.SellingPrice ?? 0)),
                batches = g.Select(b => new
                {
                    b.Id,
                    productName = b.Product.Name,
                    b.BatchNumber,
                    b.Quantity,
                    b.ExpiryDate,
                    daysUntilExpiry = (b.ExpiryDate.Date - today).Days
                }).ToList()
            });

        return Ok(new
        {
            totalActiveBatches = batches.Count,
            totalActiveQuantity = batches.Sum(b => b.Quantity),
            byExpiryRange = grouped
        });
    }
}

public class WriteOffRequest
{
    public decimal? Amount { get; set; }
    public string Reason { get; set; } = "Expired";
}
