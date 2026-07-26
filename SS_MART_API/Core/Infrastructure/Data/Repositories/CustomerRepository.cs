using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Domain.Interfaces;

namespace SS_MART_API.Core.Infrastructure.Data.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Customer>> GetAllAsync(string? search = null, string? type = null, int page = 1, int perPage = 20)
    {
        var query = _context.Customers.Where(c => c.DeletedAt == null).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Name.Contains(search) || (c.Phone != null && c.Phone.Contains(search)));
        if (!string.IsNullOrEmpty(type))
            query = query.Where(c => c.Type == type);
        return await query.OrderBy(c => c.Name).Skip((page - 1) * perPage).Take(perPage).ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(Guid id) => await _context.Customers.FindAsync(id);

    public async Task<Customer> CreateAsync(Customer customer)
    {
        customer.Id = Guid.NewGuid();
        customer.CreatedAt = DateTime.UtcNow;
        customer.UpdatedAt = DateTime.UtcNow;
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task UpdateAsync(Customer customer)
    {
        var existing = await _context.Customers.FindAsync(customer.Id);
        if (existing == null) return;
        _context.Entry(existing).CurrentValues.SetValues(customer);
        existing.UpdatedAt = DateTime.UtcNow;
        existing.Version++;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            customer.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
