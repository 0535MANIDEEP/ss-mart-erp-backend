using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Application.Interfaces;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Core.Application.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;

    public AuthService(AppDbContext context) => _context = context;

    public async Task<Employee?> ValidateCredentialsAsync(string username, string password)
        => await _context.Employees.FirstOrDefaultAsync(e => e.Username == username && e.PasswordHash == password && e.IsActive && e.DeletedAt == null);

    public async Task<Employee> RegisterAsync(string username, string password, string fullName, string role, string? phone, string? email)
    {
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = password,
            FullName = fullName,
            Role = role,
            Phone = phone,
            Email = email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        return employee;
    }

    public async Task<Employee?> GetProfileAsync(Guid id)
        => await _context.Employees.FindAsync(id);
}
