using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _context;

    public InventoryController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetStock(
        [FromQuery] string? locationId,
        [FromQuery] bool lowStockOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var query = _context.Stocks
            .Include(s => s.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(locationId))
        {
            query = query.Where(s => s.LocationId == locationId);
        }

        if (lowStockOnly)
        {
            query = query.Where(s => s.Quantity <= s.Product.ReorderLevel);
        }

        var stock = await query
            .OrderBy(s => s.Product.Name)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(s => new
            {
                productId = s.ProductId,
                productName = s.Product.Name,
                totalQuantity = s.Quantity,
                reservedQuantity = s.ReservedQuantity,
                availableQuantity = s.Quantity - s.ReservedQuantity,
                reorderLevel = s.Product.ReorderLevel,
                isLowStock = (s.Quantity - s.ReservedQuantity) <= s.Product.ReorderLevel,
                batchNumber = s.BatchNumber,
                expiryDate = s.ExpiryDate,
                locationId = s.LocationId
            })
            .ToListAsync();

        return Ok(stock);
    }

    [HttpGet("{productId}")]
    public async Task<ActionResult<object>> GetStockByProductId(Guid productId)
    {
        var stock = await _context.Stocks
            .Include(s => s.Product)
            .Where(s => s.ProductId == productId)
            .FirstOrDefaultAsync();

        if (stock == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            productId = stock.ProductId,
            productName = stock.Product.Name,
            totalQuantity = stock.Quantity,
            reservedQuantity = stock.ReservedQuantity,
            availableQuantity = stock.Quantity - stock.ReservedQuantity,
            reorderLevel = stock.Product.ReorderLevel,
            batchNumber = stock.BatchNumber,
            expiryDate = stock.ExpiryDate,
            locationId = stock.LocationId
        });
    }

    [HttpPost("adjust")]
    public async Task<ActionResult<object>> AdjustStock([FromBody] StockAdjustmentRequest request)
    {
        var stock = await _context.Stocks
            .FirstOrDefaultAsync(s => s.ProductId == request.ProductId && s.LocationId == "MAIN");

        if (stock == null)
        {
            stock = new Stock
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                LocationId = "MAIN",
                Quantity = 0,
                LastUpdated = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Stocks.Add(stock);
        }

        var previousQuantity = stock.Quantity;

        if (request.AdjustmentType.ToLower() == "add")
        {
            stock.Quantity += request.Quantity;
        }
        else if (request.AdjustmentType.ToLower() == "remove")
        {
            stock.Quantity -= request.Quantity;
        }

        stock.LastUpdated = DateTime.UtcNow;

        // Log the movement
        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            MovementType = "Adjustment",
            Quantity = request.Quantity,
            Notes = request.Reason,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.StockMovements.Add(movement);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            productId = request.ProductId,
            previousQuantity,
            adjustedQuantity = stock.Quantity,
            adjustmentType = request.AdjustmentType
        });
    }

    [HttpPost("transfer")]
    public async Task<ActionResult<object>> TransferStock([FromBody] StockTransferRequest request)
    {
        var fromStock = await _context.Stocks
            .FirstOrDefaultAsync(s => s.ProductId == request.ProductId && s.LocationId == request.FromLocationId);

        if (fromStock == null || fromStock.Quantity < request.Quantity)
        {
            return BadRequest(new { message = "Insufficient stock" });
        }

        var toStock = await _context.Stocks
            .FirstOrDefaultAsync(s => s.ProductId == request.ProductId && s.LocationId == request.ToLocationId);

        if (toStock == null)
        {
            toStock = new Stock
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                LocationId = request.ToLocationId,
                Quantity = 0,
                LastUpdated = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Stocks.Add(toStock);
        }

        fromStock.Quantity -= request.Quantity;
        fromStock.LastUpdated = DateTime.UtcNow;

        toStock.Quantity += request.Quantity;
        toStock.LastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            transferId = Guid.NewGuid(),
            productId = request.ProductId,
            quantity = request.Quantity,
            fromLocation = request.FromLocationId,
            toLocation = request.ToLocationId,
            status = "completed"
        });
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<object>>> GetLowStockProducts()
    {
        var lowStockProducts = await _context.Stocks
            .Include(s => s.Product)
            .Where(s => s.Quantity <= s.Product.ReorderLevel)
            .Select(s => new
            {
                productId = s.ProductId,
                productName = s.Product.Name,
                currentStock = s.Quantity,
                reorderLevel = s.Product.ReorderLevel,
                shortage = s.Product.ReorderLevel - s.Quantity
            })
            .ToListAsync();

        return Ok(lowStockProducts);
    }
}

public class StockAdjustmentRequest
{
    public Guid ProductId { get; set; }
    public string AdjustmentType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Reason { get; set; }
    public string? BatchNumber { get; set; }
}

public class StockTransferRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string FromLocationId { get; set; } = "MAIN";
    public string ToLocationId { get; set; } = "WAREHOUSE";
    public string? BatchNumber { get; set; }
}
