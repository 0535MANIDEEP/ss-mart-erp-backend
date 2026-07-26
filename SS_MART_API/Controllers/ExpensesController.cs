using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ExpensesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Expense>>> GetExpenses(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 50)
    {
        var query = _context.Expenses
            .Include(e => e.ExpenseCategory)
            .Where(e => e.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(e =>
                e.ExpenseNumber.Contains(search) ||
                (e.Description != null && e.Description.Contains(search)) ||
                (e.Payee != null && e.Payee.Contains(search)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(e => e.ExpenseCategoryId == categoryId.Value);
        }

        if (DateTime.TryParse(startDate, out var start))
        {
            query = query.Where(e => e.ExpenseDate >= start);
        }

        if (DateTime.TryParse(endDate, out var end))
        {
            query = query.Where(e => e.ExpenseDate <= end);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(e => e.Status == status);
        }

        var expenses = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(expenses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpense(Guid id)
    {
        var expense = await _context.Expenses
            .Include(e => e.ExpenseCategory)
            .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);

        if (expense == null)
        {
            return NotFound();
        }
        return Ok(expense);
    }

    [HttpPost]
    public async Task<ActionResult<Expense>> CreateExpense([FromBody] Expense expense)
    {
        expense.Id = Guid.NewGuid();
        expense.CreatedAt = DateTime.UtcNow;
        expense.UpdatedAt = DateTime.UtcNow;
        expense.Status = "completed";

        if (string.IsNullOrEmpty(expense.ExpenseNumber))
        {
            var count = await _context.Expenses.CountAsync(e => e.DeletedAt == null);
            expense.ExpenseNumber = $"EXP-{(count + 1).ToString("D4")}";
        }

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, expense);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExpense(Guid id, [FromBody] Expense expense)
    {
        var existing = await _context.Expenses.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.ExpenseCategoryId = expense.ExpenseCategoryId;
        existing.ExpenseDate = expense.ExpenseDate;
        existing.Amount = expense.Amount;
        existing.PaymentMode = expense.PaymentMode;
        existing.Payee = expense.Payee;
        existing.Description = expense.Description;
        existing.IsRecurring = expense.IsRecurring;
        existing.RecurringFrequency = expense.RecurringFrequency;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(Guid id)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null)
        {
            return NotFound();
        }

        expense.DeletedAt = DateTime.UtcNow;
        expense.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetExpenseSummary(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate)
    {
        var query = _context.Expenses
            .Where(e => e.DeletedAt == null)
            .AsQueryable();

        if (DateTime.TryParse(startDate, out var start))
        {
            query = query.Where(e => e.ExpenseDate >= start);
        }

        if (DateTime.TryParse(endDate, out var end))
        {
            query = query.Where(e => e.ExpenseDate <= end);
        }

        var expenses = await query
            .Include(e => e.ExpenseCategory)
            .ToListAsync();

        var totalAmount = expenses.Sum(e => e.Amount);

        var byCategory = expenses
            .GroupBy(e => new
            {
                CategoryId = e.ExpenseCategoryId,
                CategoryName = e.ExpenseCategory?.Name ?? "Uncategorized"
            })
            .Select(g => new
            {
                categoryId = g.Key.CategoryId,
                categoryName = g.Key.CategoryName,
                total = g.Sum(e => e.Amount),
                count = g.Count()
            })
            .OrderByDescending(c => c.total)
            .ToList();

        var byPaymentMode = expenses
            .GroupBy(e => e.PaymentMode)
            .Select(g => new
            {
                paymentMode = g.Key,
                total = g.Sum(e => e.Amount),
                count = g.Count()
            })
            .OrderByDescending(p => p.total)
            .ToList();

        var dailyBreakdown = expenses
            .GroupBy(e => e.ExpenseDate.Date)
            .Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                total = g.Sum(e => e.Amount),
                count = g.Count()
            })
            .OrderBy(d => d.date)
            .ToList();

        return Ok(new
        {
            summary = new
            {
                totalExpenses = expenses.Count,
                totalAmount,
                averageExpense = expenses.Any() ? totalAmount / expenses.Count : 0
            },
            byCategory,
            byPaymentMode,
            dailyBreakdown
        });
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<ExpenseCategory>>> GetExpenseCategories()
    {
        var categories = await _context.ExpenseCategories
            .Where(c => c.DeletedAt == null && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost("categories")]
    public async Task<ActionResult<ExpenseCategory>> CreateExpenseCategory(
        [FromBody] ExpenseCategory category)
    {
        category.Id = Guid.NewGuid();
        category.CreatedAt = DateTime.UtcNow;
        category.UpdatedAt = DateTime.UtcNow;

        _context.ExpenseCategories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetExpenseCategories), category);
    }
}
