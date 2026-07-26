using SS_MART_API.Core.Application.Interfaces;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Domain.Interfaces;

namespace SS_MART_API.Core.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;

    public CustomerService(ICustomerRepository repository) => _repository = repository;

    public Task<IEnumerable<Customer>> GetCustomersAsync(string? search = null, string? type = null, int page = 1, int perPage = 20)
        => _repository.GetAllAsync(search, type, page, perPage);

    public Task<Customer?> GetCustomerByIdAsync(Guid id) => _repository.GetByIdAsync(id);

    public Task<Customer> CreateCustomerAsync(Customer customer) => _repository.CreateAsync(customer);

    public Task UpdateCustomerAsync(Guid id, Customer customer) => _repository.UpdateAsync(customer);

    public Task DeleteCustomerAsync(Guid id) => _repository.DeleteAsync(id);
}
