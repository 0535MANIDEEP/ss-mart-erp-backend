using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Application.Interfaces;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Core.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;

    public InventoryService(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Stock>> GetStockAsync(string? locationId = null, bool lowStockOnly = false, int page = 1, int perPage = 20)
    {
        var query = _context.Stocks.Include(s => s.Product).AsQueryable();
        if (!string.IsNullOrEmpty(locationId)) query = query.Where(s => s.LocationId == locationId);
        if (lowStockOnly) query = query.Where(s => s.Quantity <= s.Product.ReorderLevel);
        return await query.OrderBy(s => s.Product.Name).Skip((page - 1) * perPage).Take(perPage).ToListAsync();
    }

    public async Task<Stock?> GetStockByProductIdAsync(Guid productId)
        => await _context.Stocks.Include(s => s.Product).FirstOrDefaultAsync(s => s.ProductId == productId);

    public async Task<Stock> AdjustStockAsync(Guid productId, string adjustmentType, int quantity, string? reason)
    {
        var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.ProductId == productId && s.LocationId == "MAIN");
        if (stock == null)
        {
            stock = new Stock { Id = Guid.NewGuid(), ProductId = productId, LocationId = "MAIN", Quantity = 0, LastUpdated = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.Stocks.Add(stock);
        }
        if (adjustmentType.ToLower() == "add") stock.Quantity += quantity;
        else if (adjustmentType.ToLower() == "remove") stock.Quantity -= quantity;
        stock.LastUpdated = DateTime.UtcNow;
        _context.StockMovements.Add(new StockMovement { Id = Guid.NewGuid(), ProductId = productId, MovementType = "Adjustment", Quantity = quantity, Notes = reason, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        return stock;
    }

    public async Task TransferStockAsync(Guid productId, int quantity, string fromLocationId, string toLocationId)
    {
        var fromStock = await _context.Stocks.FirstOrDefaultAsync(s => s.ProductId == productId && s.LocationId == fromLocationId);
        if (fromStock == null || fromStock.Quantity < quantity) throw new InvalidOperationException("Insufficient stock");
        var toStock = await _context.Stocks.FirstOrDefaultAsync(s => s.ProductId == productId && s.LocationId == toLocationId);
        if (toStock == null)
        {
            toStock = new Stock { Id = Guid.NewGuid(), ProductId = productId, LocationId = toLocationId, Quantity = 0, LastUpdated = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.Stocks.Add(toStock);
        }
        fromStock.Quantity -= quantity;
        fromStock.LastUpdated = DateTime.UtcNow;
        toStock.Quantity += quantity;
        toStock.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Stock>> GetLowStockProductsAsync()
        => await _context.Stocks.Include(s => s.Product).Where(s => s.Quantity <= s.Product.ReorderLevel).ToListAsync();
}
