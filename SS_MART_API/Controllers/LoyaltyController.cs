using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoyaltyController : ControllerBase
{
    private readonly AppDbContext _context;

    public LoyaltyController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{customerId}")]
    public async Task<ActionResult<object>> GetLoyaltyBalance(Guid customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
        {
            return NotFound();
        }

        var totalEarned = await _context.LoyaltyTransactions
            .Where(l => l.CustomerId == customerId && l.TransactionType == "earn")
            .SumAsync(l => l.Points);

        var totalRedeemed = await _context.LoyaltyTransactions
            .Where(l => l.CustomerId == customerId && l.TransactionType == "redeem")
            .SumAsync(l => l.Points);

        var expiringPoints = await _context.LoyaltyTransactions
            .Where(l => l.CustomerId == customerId &&
                       l.TransactionType == "earn" &&
                       l.ExpiryDate != null &&
                       l.ExpiryDate <= DateTime.UtcNow.AddDays(30) &&
                       l.ExpiryDate > DateTime.UtcNow)
            .SumAsync(l => l.Points);

        return Ok(new
        {
            customerId = customer.Id,
            customerName = customer.Name,
            totalPointsEarned = totalEarned,
            totalPointsRedeemed = totalRedeemed,
            currentBalance = customer.LoyaltyPoints,
            pendingPoints = 0,
            expiringPoints,
            nextExpiryDate = await _context.LoyaltyTransactions
                .Where(l => l.CustomerId == customerId &&
                           l.TransactionType == "earn" &&
                           l.ExpiryDate != null &&
                           l.ExpiryDate > DateTime.UtcNow)
                .OrderBy(l => l.ExpiryDate)
                .Select(l => l.ExpiryDate)
                .FirstOrDefaultAsync()
        });
    }

    [HttpPost("earn")]
    public async Task<ActionResult<object>> EarnPoints([FromBody] EarnPointsRequest request)
    {
        var customer = await _context.Customers.FindAsync(request.CustomerId);
        if (customer == null)
        {
            return NotFound();
        }

        var transaction = new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            TransactionType = "earn",
            Points = request.Points,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            ExpiryDate = DateTime.UtcNow.AddDays(365),
            Notes = request.Notes,
            CreatedBy = request.CustomerId, // Should be current user
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.LoyaltyTransactions.Add(transaction);
        customer.LoyaltyPoints += request.Points;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            transactionId = transaction.Id,
            customerId = customer.Id,
            pointsEarned = request.Points,
            newBalance = customer.LoyaltyPoints,
            expiryDate = transaction.ExpiryDate
        });
    }

    [HttpPost("redeem")]
    public async Task<ActionResult<object>> RedeemPoints([FromBody] RedeemPointsRequest request)
    {
        var customer = await _context.Customers.FindAsync(request.CustomerId);
        if (customer == null)
        {
            return NotFound();
        }

        if (customer.LoyaltyPoints < request.Points)
        {
            return BadRequest(new { message = "Insufficient loyalty points" });
        }

        var transaction = new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            TransactionType = "redeem",
            Points = request.Points,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            Notes = request.Notes,
            CreatedBy = request.CustomerId, // Should be current user
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.LoyaltyTransactions.Add(transaction);
        customer.LoyaltyPoints -= request.Points;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Calculate discount amount (1 point = 1 rupee)
        var discountAmount = request.Points;

        return Ok(new
        {
            transactionId = transaction.Id,
            customerId = customer.Id,
            pointsRedeemed = request.Points,
            discountAmount,
            newBalance = customer.LoyaltyPoints
        });
    }

    [HttpGet("history/{customerId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetLoyaltyHistory(
        Guid customerId,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var transactions = await _context.LoyaltyTransactions
            .Where(l => l.CustomerId == customerId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(l => new
            {
                l.Id,
                l.TransactionType,
                l.Points,
                l.ReferenceType,
                l.ReferenceId,
                l.ExpiryDate,
                l.Notes,
                l.CreatedAt
            })
            .ToListAsync();

        return Ok(transactions);
    }
}

public class EarnPointsRequest
{
    public Guid CustomerId { get; set; }
    public int Points { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
}

public class RedeemPointsRequest
{
    public Guid CustomerId { get; set; }
    public int Points { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
}
