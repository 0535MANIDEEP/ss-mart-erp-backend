using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SettingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Setting>>> GetSettings(
        [FromQuery] string? category,
        [FromQuery] string? search)
    {
        var query = _context.Settings
            .Where(s => s.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(s => s.Category == category);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s => s.Key.Contains(search));
        }

        var settings = await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .ToListAsync();

        return Ok(settings);
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<Setting>> GetSetting(string key)
    {
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == key && s.DeletedAt == null);

        if (setting == null)
        {
            return NotFound();
        }
        return Ok(setting);
    }

    [HttpPost]
    public async Task<ActionResult<Setting>> CreateSetting(Setting setting)
    {
        setting.Id = Guid.NewGuid();
        setting.CreatedAt = DateTime.UtcNow;
        setting.UpdatedAt = DateTime.UtcNow;
        _context.Settings.Add(setting);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSetting), new { key = setting.Key }, setting);
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> UpdateSetting(string key, Setting setting)
    {
        var existing = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == key && s.DeletedAt == null);

        if (existing == null)
        {
            return NotFound();
        }

        existing.Value = setting.Value;
        existing.Category = setting.Category;
        existing.Description = setting.Description;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.Version++;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> DeleteSetting(string key)
    {
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == key && s.DeletedAt == null);

        if (setting == null)
        {
            return NotFound();
        }

        setting.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("bulk")]
    public async Task<IActionResult> BulkUpdate([FromBody] Dictionary<string, string> updates)
    {
        foreach (var kvp in updates)
        {
            var existing = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == kvp.Key && s.DeletedAt == null);

            if (existing != null)
            {
                existing.Value = kvp.Value;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.Version++;
            }
            else
            {
                _context.Settings.Add(new Setting
                {
                    Id = Guid.NewGuid(),
                    Key = kvp.Key,
                    Value = kvp.Value,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
