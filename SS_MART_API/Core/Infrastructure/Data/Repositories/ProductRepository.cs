using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Domain.Interfaces;

namespace SS_MART_API.Core.Infrastructure.Data.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Product>> GetAllAsync(string? search = null, int page = 1, int perPage = 20)
    {
        var query = _context.Products.Where(p => p.DeletedAt == null).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.Contains(search) || (p.SKU != null && p.SKU.Contains(search)) || (p.Barcode != null && p.Barcode.Contains(search)));
        return await query.OrderBy(p => p.Name).Skip((page - 1) * perPage).Take(perPage).ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(Guid id) => await _context.Products.FindAsync(id);

    public async Task<Product?> GetByBarcodeAsync(string barcode)
        => await _context.Products.FirstOrDefaultAsync(p => p.Barcode == barcode && p.DeletedAt == null);

    public async Task<Product> CreateAsync(Product product)
    {
        product.Id = Guid.NewGuid();
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        var existing = await _context.Products.FindAsync(product.Id);
        if (existing == null) return;
        _context.Entry(existing).CurrentValues.SetValues(product);
        existing.UpdatedAt = DateTime.UtcNow;
        existing.Version++;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            product.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
