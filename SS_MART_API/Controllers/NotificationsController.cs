using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("templates")]
    public async Task<ActionResult<IEnumerable<NotificationTemplate>>> GetTemplates(
        [FromQuery] string? type,
        [FromQuery] string? eventFilter)
    {
        var query = _context.NotificationTemplates
            .Where(t => t.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(t => t.Type == type);
        }

        if (!string.IsNullOrEmpty(eventFilter))
        {
            query = query.Where(t => t.Event == eventFilter);
        }

        var templates = await query.OrderBy(t => t.Name).ToListAsync();
        return Ok(templates);
    }

    [HttpGet("templates/{id}")]
    public async Task<ActionResult<NotificationTemplate>> GetTemplate(Guid id)
    {
        var template = await _context.NotificationTemplates.FindAsync(id);
        if (template == null) return NotFound();
        return Ok(template);
    }

    [HttpPost("templates")]
    public async Task<ActionResult<NotificationTemplate>> CreateTemplate(
        [FromBody] NotificationTemplate template)
    {
        template.Id = Guid.NewGuid();
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;

        _context.NotificationTemplates.Add(template);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
    }

    [HttpPut("templates/{id}")]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] NotificationTemplate template)
    {
        var existing = await _context.NotificationTemplates.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = template.Name;
        existing.Type = template.Type;
        existing.Event = template.Event;
        existing.Subject = template.Subject;
        existing.Body = template.Body;
        existing.IsActive = template.IsActive;
        existing.Description = template.Description;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("templates/{id}")]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        var template = await _context.NotificationTemplates.FindAsync(id);
        if (template == null) return NotFound();

        template.DeletedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("logs")]
    public async Task<ActionResult<IEnumerable<NotificationLog>>> GetLogs(
        [FromQuery] string? channel,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 50)
    {
        var query = _context.NotificationLogs
            .Where(l => l.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(channel))
            query = query.Where(l => l.Channel == channel);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(l => l.Status == status);

        var logs = await query
            .OrderByDescending(l => l.SentAt ?? l.CreatedAt)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(logs);
    }

    [HttpPost("preview")]
    public ActionResult<object> PreviewTemplate([FromBody] PreviewRequest request)
    {
        var body = request.Body;
        foreach (var kv in request.Variables)
        {
            body = body.Replace($"{{{{{kv.Key}}}}}", kv.Value);
        }

        return Ok(new { preview = body });
    }

    [HttpGet("config")]
    public async Task<ActionResult<object>> GetNotificationConfig()
    {
        var templates = await _context.NotificationTemplates
            .Where(t => t.DeletedAt == null)
            .ToListAsync();

        return Ok(new
        {
            smsEnabled = templates.Any(t => t.Type == "sms" && t.IsActive),
            emailEnabled = templates.Any(t => t.Type == "email" && t.IsActive),
            whatsappEnabled = templates.Any(t => t.Type == "whatsapp" && t.IsActive),
            templateCount = templates.Count,
            activeTemplateCount = templates.Count(t => t.IsActive),
            events = templates.Select(t => t.Event).Distinct().ToList()
        });
    }
}

public class PreviewRequest
{
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string> Variables { get; set; } = new();
}
