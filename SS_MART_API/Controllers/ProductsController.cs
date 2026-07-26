using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var query = _context.Products
            .Where(p => p.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                p.Name.Contains(search) ||
                (p.SKU != null && p.SKU.Contains(search)) ||
                (p.Barcode != null && p.Barcode.Contains(search)));
        }

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        product.Id = Guid.NewGuid();
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, Product product)
    {
        if (id != product.Id)
        {
            return BadRequest();
        }

        var existingProduct = await _context.Products.FindAsync(id);
        if (existingProduct == null)
        {
            return NotFound();
        }

        existingProduct.Name = product.Name;
        existingProduct.SKU = product.SKU;
        existingProduct.Barcode = product.Barcode;
        existingProduct.HSNCode = product.HSNCode;
        existingProduct.Unit = product.Unit;
        existingProduct.PackSize = product.PackSize;
        existingProduct.MRP = product.MRP;
        existingProduct.SellingPrice = product.SellingPrice;
        existingProduct.PurchasePrice = product.PurchasePrice;
        existingProduct.TaxRate = product.TaxRate;
        existingProduct.TaxType = product.TaxType;
        existingProduct.CategoryId = product.CategoryId;
        existingProduct.SupplierId = product.SupplierId;
        existingProduct.ReorderLevel = product.ReorderLevel;
        existingProduct.CurrentStock = product.CurrentStock;
        existingProduct.IsActive = product.IsActive;
        existingProduct.UpdatedAt = DateTime.UtcNow;
        existingProduct.Version++;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        product.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<Product>> GetProductByBarcode(string barcode)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Barcode == barcode && p.DeletedAt == null);

        if (product == null)
        {
            return NotFound();
        }
        return Ok(product);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Product>>> SearchProducts(
        [FromQuery] string q,
        [FromQuery] int limit = 20)
    {
        var products = await _context.Products
            .Where(p => p.DeletedAt == null && (
                p.Name.Contains(q) ||
                (p.SKU != null && p.SKU.Contains(q)) ||
                (p.Barcode != null && p.Barcode.Contains(q))))
            .Take(limit)
            .ToListAsync();

        return Ok(products);
    }
}
