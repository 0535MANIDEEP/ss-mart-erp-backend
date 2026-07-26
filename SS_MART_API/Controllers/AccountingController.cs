using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountingController : ControllerBase
{
    private readonly AppDbContext _context;

    public AccountingController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("entries")]
    public async Task<ActionResult<IEnumerable<LedgerEntry>>> GetEntries(
        [FromQuery] string? accountHead,
        [FromQuery] string? entryType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var query = _context.LedgerEntries
            .Where(e => e.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(accountHead))
        {
            query = query.Where(e => e.AccountHead == accountHead);
        }

        if (!string.IsNullOrEmpty(entryType))
        {
            query = query.Where(e => e.EntryType == entryType);
        }

        if (from.HasValue)
        {
            query = query.Where(e => e.EntryDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.EntryDate <= to.Value);
        }

        var entries = await query
            .OrderByDescending(e => e.EntryDate)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(entries);
    }

    [HttpGet("entries/{id}")]
    public async Task<ActionResult<LedgerEntry>> GetEntry(Guid id)
    {
        var entry = await _context.LedgerEntries.FindAsync(id);
        if (entry == null)
        {
            return NotFound();
        }
        return Ok(entry);
    }

    [HttpPost("entries")]
    public async Task<ActionResult<LedgerEntry>> CreateEntry(LedgerEntry entry)
    {
        entry.Id = Guid.NewGuid();
        entry.CreatedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;
        _context.LedgerEntries.Add(entry);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEntry), new { id = entry.Id }, entry);
    }

    [HttpDelete("entries/{id}")]
    public async Task<IActionResult> DeleteEntry(Guid id)
    {
        var entry = await _context.LedgerEntries.FindAsync(id);
        if (entry == null)
        {
            return NotFound();
        }

        entry.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("trial-balance")]
    public async Task<ActionResult<object>> GetTrialBalance(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = _context.LedgerEntries
            .Where(e => e.DeletedAt == null)
            .AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(e => e.EntryDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.EntryDate <= to.Value);
        }

        var entries = await query.ToListAsync();

        var debitTotal = entries.Where(e => e.EntryType == "debit").Sum(e => e.Amount);
        var creditTotal = entries.Where(e => e.EntryType == "credit").Sum(e => e.Amount);

        var byAccount = entries
            .GroupBy(e => e.AccountHead)
            .Select(g => new
            {
                accountHead = g.Key,
                debit = g.Where(e => e.EntryType == "debit").Sum(e => e.Amount),
                credit = g.Where(e => e.EntryType == "credit").Sum(e => e.Amount)
            })
            .OrderBy(x => x.accountHead)
            .ToList();

        return Ok(new
        {
            totalDebit = debitTotal,
            totalCredit = creditTotal,
            difference = debitTotal - creditTotal,
            accounts = byAccount
        });
    }
}
