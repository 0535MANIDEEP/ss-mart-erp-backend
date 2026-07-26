using SS_MART_API.Core.Domain.Entities;

namespace SS_MART_API.Core.Application.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<Customer>> GetCustomersAsync(string? search = null, string? type = null, int page = 1, int perPage = 20);
    Task<Customer?> GetCustomerByIdAsync(Guid id);
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task UpdateCustomerAsync(Guid id, Customer customer);
    Task DeleteCustomerAsync(Guid id);
}
