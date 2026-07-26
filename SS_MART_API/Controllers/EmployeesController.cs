using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var query = _context.Employees
            .Where(e => e.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(e =>
                e.FullName.Contains(search) ||
                e.Username.Contains(search) ||
                (e.Phone != null && e.Phone.Contains(search)));
        }

        var employees = await query
            .OrderBy(e => e.FullName)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(employees);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Employee>> GetEmployee(Guid id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            return NotFound();
        }
        return Ok(employee);
    }

    [HttpPost]
    public async Task<ActionResult<Employee>> CreateEmployee(Employee employee)
    {
        employee.Id = Guid.NewGuid();
        employee.CreatedAt = DateTime.UtcNow;
        employee.UpdatedAt = DateTime.UtcNow;
        // TODO: Hash password
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(Guid id, Employee employee)
    {
        if (id != employee.Id)
        {
            return BadRequest();
        }

        var existingEmployee = await _context.Employees.FindAsync(id);
        if (existingEmployee == null)
        {
            return NotFound();
        }

        existingEmployee.FullName = employee.FullName;
        existingEmployee.Phone = employee.Phone;
        existingEmployee.Email = employee.Email;
        existingEmployee.Role = employee.Role;
        existingEmployee.IsActive = employee.IsActive;
        existingEmployee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(Guid id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            return NotFound();
        }

        employee.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/clock-in")]
    public async Task<ActionResult<Attendance>> ClockIn(Guid id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            return NotFound();
        }

        var today = DateTime.UtcNow.Date;
        var existingAttendance = await _context.Attendances
            .FirstOrDefaultAsync(a =>
                a.EmployeeId == id &&
                a.Date.Date == today);

        if (existingAttendance != null && existingAttendance.ClockOutTime == null)
        {
            return BadRequest(new { message = "Already clocked in today" });
        }

        var attendance = new Attendance
        {
            Id = Guid.NewGuid(),
            EmployeeId = id,
            Date = DateTime.UtcNow,
            ClockInTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync();

        return Ok(attendance);
    }

    [HttpPost("{id}/clock-out")]
    public async Task<ActionResult<Attendance>> ClockOut(Guid id)
    {
        var today = DateTime.UtcNow.Date;
        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a =>
                a.EmployeeId == id &&
                a.Date.Date == today &&
                a.ClockOutTime == null);

        if (attendance == null)
        {
            return NotFound(new { message = "No active clock-in found" });
        }

        attendance.ClockOutTime = DateTime.UtcNow;
        attendance.HoursWorked = (attendance.ClockOutTime - attendance.ClockInTime).TotalHours;
        attendance.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(attendance);
    }

    [HttpGet("{id}/attendance")]
    public async Task<ActionResult<IEnumerable<Attendance>>> GetAttendance(
        Guid id,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = _context.Attendances
            .Where(a => a.EmployeeId == id)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Date <= endDate.Value);
        }

        var attendance = await query
            .OrderByDescending(a => a.Date)
            .ToListAsync();

        return Ok(attendance);
    }
}
