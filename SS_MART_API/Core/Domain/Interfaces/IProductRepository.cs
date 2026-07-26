using SS_MART_API.Core.Domain.Entities;

namespace SS_MART_API.Core.Domain.Interfaces;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync(string? search = null, int page = 1, int perPage = 20);
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<Product> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Guid id);
}
