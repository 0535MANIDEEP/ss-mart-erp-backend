using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e =>
                e.Username == request.Username &&
                e.IsActive &&
                e.DeletedAt == null);

        if (employee == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // TODO: Implement proper password hashing (BCrypt)
        // For now, plain text comparison
        if (employee.PasswordHash != request.Password)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var token = GenerateJwtToken(employee);

        return Ok(new LoginResponse
        {
            Token = token,
            Employee = employee
        });
    }

    [HttpPost("register")]
    public async Task<ActionResult<Employee>> Register(RegisterRequest request)
    {
        if (await _context.Employees.AnyAsync(e => e.Username == request.Username))
        {
            return BadRequest(new { message = "Username already exists" });
        }

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = request.Password, // TODO: Hash password
            FullName = request.FullName,
            Role = request.Role,
            Phone = request.Phone,
            Email = request.Email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProfile), new { id = employee.Id }, employee);
    }

    [HttpGet("profile/{id}")]
    public async Task<ActionResult<Employee>> GetProfile(Guid id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            return NotFound();
        }
        return Ok(employee);
    }

    [HttpPost("refresh")]
    public ActionResult<LoginResponse> RefreshToken(RefreshRequest request)
    {
        // TODO: Implement refresh token logic
        return StatusCode(501);
    }

    private string GenerateJwtToken(Employee employee)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Key"] ?? "SSMartDefaultSecretKey12345678901234";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, employee.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("username", employee.Username),
            new Claim("fullName", employee.FullName),
            new Claim("role", employee.Role.ToString()),
            new Claim("storeId", employee.StoreId?.ToString() ?? ""),
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryInMinutes"] ?? "60")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public Employee Employee { get; set; } = null!;
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Cashier";
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
