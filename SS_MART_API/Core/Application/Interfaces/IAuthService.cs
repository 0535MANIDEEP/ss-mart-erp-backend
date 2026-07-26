using SS_MART_API.Core.Domain.Entities;

namespace SS_MART_API.Core.Application.Interfaces;

public interface IAuthService
{
    Task<Employee?> ValidateCredentialsAsync(string username, string password);
    Task<Employee> RegisterAsync(string username, string password, string fullName, string role, string? phone, string? email);
    Task<Employee?> GetProfileAsync(Guid id);
}
