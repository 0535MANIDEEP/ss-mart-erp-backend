using SS_MART_API.Core.Domain.Entities;

namespace SS_MART_API.Core.Application.Interfaces;

public interface IProductService
{
    Task<IEnumerable<Product>> GetProductsAsync(string? search = null, int page = 1, int perPage = 20);
    Task<Product?> GetProductByIdAsync(Guid id);
    Task<Product?> GetProductByBarcodeAsync(string barcode);
    Task<Product> CreateProductAsync(Product product);
    Task UpdateProductAsync(Guid id, Product product);
    Task DeleteProductAsync(Guid id);
}
