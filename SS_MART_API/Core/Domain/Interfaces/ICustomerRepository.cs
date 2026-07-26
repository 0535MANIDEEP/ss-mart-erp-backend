using SS_MART_API.Core.Domain.Entities;

namespace SS_MART_API.Core.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetAllAsync(string? search = null, string? type = null, int page = 1, int perPage = 20);
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer> CreateAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(Guid id);
}
