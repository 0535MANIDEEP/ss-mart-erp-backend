using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DayEndController : ControllerBase
{
    private readonly AppDbContext _context;

    public DayEndController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<IEnumerable<DayEndSession>>> GetSessions(
        [FromQuery] string? status,
        [FromQuery] Guid? employeeId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 50)
    {
        var query = _context.DayEndSessions
            .Include(s => s.Employee)
            .Where(s => s.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);
        if (employeeId.HasValue)
            query = query.Where(s => s.EmployeeId == employeeId.Value);
        if (from.HasValue)
            query = query.Where(s => s.OpenedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(s => s.OpenedAt <= to.Value);

        var sessions = await query
            .OrderByDescending(s => s.OpenedAt)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(sessions);
    }

    [HttpGet("sessions/{id}")]
    public async Task<ActionResult<DayEndSession>> GetSession(Guid id)
    {
        var session = await _context.DayEndSessions
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null) return NotFound();

        var transactions = await _context.DayEndTransactions
            .Where(t => t.SessionId == id && t.DeletedAt == null)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        return Ok(new
        {
            session,
            transactions
        });
    }

    [HttpPost("open")]
    public async Task<ActionResult<DayEndSession>> OpenSession([FromBody] OpenSessionRequest request)
    {
        var openExists = await _context.DayEndSessions
            .AnyAsync(s => s.EmployeeId == request.EmployeeId && s.Status == "open" && s.DeletedAt == null);

        if (openExists)
            return BadRequest(new { error = "Employee already has an open session" });

        var session = new DayEndSession
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            OpenedAt = DateTime.UtcNow,
            OpeningBalance = request.OpeningBalance,
            ExpectedCash = request.OpeningBalance,
            ActualCash = 0,
            TotalSales = 0,
            TotalReturns = 0,
            TotalPaymentsMade = 0,
            BillCount = 0,
            Status = "open",
            DenominationSnapshot = request.DenominationSnapshot,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DayEndSessions.Add(session);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSession), new { id = session.Id }, session);
    }

    [HttpPost("sessions/{id}/close")]
    public async Task<IActionResult> CloseSession(Guid id, [FromBody] CloseSessionRequest request)
    {
        var session = await _context.DayEndSessions.FindAsync(id);
        if (session == null) return NotFound();
        if (session.Status != "open") return BadRequest(new { error = "Session is not open" });

        session.ClosedAt = DateTime.UtcNow;
        session.ClosingBalance = request.ClosingBalance;
        session.ActualCash = request.ActualCash;
        session.Discrepancy = request.ActualCash - session.ExpectedCash;
        session.Notes = request.Notes;
        session.DenominationSnapshot = request.DenominationSnapshot;
        session.Status = "closed";
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("sessions/{id}/reconcile")]
    public async Task<IActionResult> ReconcileSession(Guid id)
    {
        var session = await _context.DayEndSessions.FindAsync(id);
        if (session == null) return NotFound();
        if (session.Status != "closed") return BadRequest(new { error = "Session must be closed first" });

        session.Status = "reconciled";
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("sessions/{id}/transactions")]
    public async Task<ActionResult<IEnumerable<DayEndTransaction>>> GetTransactions(
        Guid id,
        [FromQuery] string? type)
    {
        var query = _context.DayEndTransactions
            .Where(t => t.SessionId == id && t.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(type))
            query = query.Where(t => t.TransactionType == type);

        return Ok(await query.OrderBy(t => t.CreatedAt).ToListAsync());
    }

    [HttpPost("sessions/{id}/transactions")]
    public async Task<ActionResult<DayEndTransaction>> AddTransaction(
        Guid id,
        [FromBody] DayEndTransaction transaction)
    {
        var session = await _context.DayEndSessions.FindAsync(id);
        if (session == null) return NotFound();
        if (session.Status != "open") return BadRequest(new { error = "Session is not open" });

        transaction.Id = Guid.NewGuid();
        transaction.SessionId = id;
        transaction.CreatedAt = DateTime.UtcNow;
        transaction.UpdatedAt = DateTime.UtcNow;

        _context.DayEndTransactions.Add(transaction);

        switch (transaction.TransactionType)
        {
            case "sale":
                session.TotalSales += transaction.Amount;
                session.BillCount++;
                session.ExpectedCash += transaction.PaymentMode == "CASH" ? transaction.Amount : 0;
                break;
            case "return":
                session.TotalReturns += transaction.Amount;
                session.ExpectedCash -= transaction.PaymentMode == "CASH" ? transaction.Amount : 0;
                break;
            case "payment_out":
                session.TotalPaymentsMade += transaction.Amount;
                session.ExpectedCash -= transaction.PaymentMode == "CASH" ? transaction.Amount : 0;
                break;
            case "adjustment":
                session.ExpectedCash += transaction.Amount;
                break;
        }

        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTransactions), new { id }, transaction);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetSummary([FromQuery] DateTime? date)
    {
        var targetDate = date ?? DateTime.UtcNow.Date;
        var startOfDay = targetDate;
        var endOfDay = targetDate.AddDays(1);

        var sessions = await _context.DayEndSessions
            .Include(s => s.Employee)
            .Where(s => s.OpenedAt >= startOfDay && s.OpenedAt < endOfDay && s.DeletedAt == null)
            .ToListAsync();

        return Ok(new
        {
            date = targetDate.Date,
            totalSessions = sessions.Count,
            openSessions = sessions.Count(s => s.Status == "open"),
            closedSessions = sessions.Count(s => s.Status == "closed"),
            reconciledSessions = sessions.Count(s => s.Status == "reconciled"),
            totalSales = sessions.Sum(s => s.TotalSales),
            totalReturns = sessions.Sum(s => s.TotalReturns),
            totalPaymentsMade = sessions.Sum(s => s.TotalPaymentsMade),
            totalDiscrepancy = sessions.Where(s => s.Discrepancy.HasValue).Sum(s => s.Discrepancy!.Value),
            sessions = sessions.Select(s => new
            {
                employee = s.Employee.FullName,
                status = s.Status,
                expectedCash = s.ExpectedCash,
                actualCash = s.ActualCash,
                discrepancy = s.Discrepancy
            })
        });
    }
}

public class OpenSessionRequest
{
    public Guid EmployeeId { get; set; }
    public decimal OpeningBalance { get; set; }
    public string? DenominationSnapshot { get; set; }
}

public class CloseSessionRequest
{
    public decimal ClosingBalance { get; set; }
    public decimal ActualCash { get; set; }
    public string? Notes { get; set; }
    public string? DenominationSnapshot { get; set; }
}
