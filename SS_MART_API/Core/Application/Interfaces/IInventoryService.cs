using SS_MART_API.Core.Domain.Entities;

namespace SS_MART_API.Core.Application.Interfaces;

public interface IInventoryService
{
    Task<IEnumerable<Stock>> GetStockAsync(string? locationId = null, bool lowStockOnly = false, int page = 1, int perPage = 20);
    Task<Stock?> GetStockByProductIdAsync(Guid productId);
    Task<Stock> AdjustStockAsync(Guid productId, string adjustmentType, int quantity, string? reason);
    Task TransferStockAsync(Guid productId, int quantity, string fromLocationId, string toLocationId);
    Task<IEnumerable<Stock>> GetLowStockProductsAsync();
}
