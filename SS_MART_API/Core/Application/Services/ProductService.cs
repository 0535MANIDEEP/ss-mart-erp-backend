using SS_MART_API.Core.Application.Interfaces;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Domain.Interfaces;

namespace SS_MART_API.Core.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository) => _repository = repository;

    public Task<IEnumerable<Product>> GetProductsAsync(string? search = null, int page = 1, int perPage = 20)
        => _repository.GetAllAsync(search, page, perPage);

    public Task<Product?> GetProductByIdAsync(Guid id) => _repository.GetByIdAsync(id);

    public Task<Product?> GetProductByBarcodeAsync(string barcode) => _repository.GetByBarcodeAsync(barcode);

    public Task<Product> CreateProductAsync(Product product) => _repository.CreateAsync(product);

    public Task UpdateProductAsync(Guid id, Product product) => _repository.UpdateAsync(product);

    public Task DeleteProductAsync(Guid id) => _repository.DeleteAsync(id);
}
