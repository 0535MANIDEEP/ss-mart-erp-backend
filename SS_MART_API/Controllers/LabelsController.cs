using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Infrastructure.Data;

namespace SS_MART_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LabelsController : ControllerBase
{
    private readonly AppDbContext _context;

    public LabelsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LabelTemplate>>> GetLabelTemplates(
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 50)
    {
        var query = _context.LabelTemplates
            .Where(l => l.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(l => l.Type == type);
        }

        var templates = await query
            .OrderBy(l => l.Name)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(templates);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LabelTemplate>> GetLabelTemplate(Guid id)
    {
        var template = await _context.LabelTemplates.FindAsync(id);
        if (template == null)
        {
            return NotFound();
        }
        return Ok(template);
    }

    [HttpPost]
    public async Task<ActionResult<LabelTemplate>> CreateLabelTemplate(LabelTemplate template)
    {
        template.Id = Guid.NewGuid();
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        _context.LabelTemplates.Add(template);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetLabelTemplate), new { id = template.Id }, template);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLabelTemplate(Guid id, LabelTemplate template)
    {
        if (id != template.Id)
        {
            return BadRequest();
        }

        var existing = await _context.LabelTemplates.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Name = template.Name;
        existing.Type = template.Type;
        existing.Width = template.Width;
        existing.Height = template.Height;
        existing.Layout = template.Layout;
        existing.IsDefault = template.IsDefault;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.Version++;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLabelTemplate(Guid id)
    {
        var template = await _context.LabelTemplates.FindAsync(id);
        if (template == null)
        {
            return NotFound();
        }

        template.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("print")]
    public async Task<ActionResult<object>> PrintLabels([FromBody] PrintLabelRequest request)
    {
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
        {
            return NotFound("Product not found");
        }

        LabelTemplate? template = null;
        if (request.TemplateId.HasValue)
        {
            template = await _context.LabelTemplates.FindAsync(request.TemplateId.Value);
        }

        template ??= await _context.LabelTemplates
            .FirstOrDefaultAsync(l => l.Type == request.Type && l.IsDefault && l.DeletedAt == null);

        return Ok(new
        {
            product = new { product.Name, product.SKU, product.Barcode, product.MRP, product.SellingPrice },
            template = template != null ? new { template.Name, template.Type, template.Width, template.Height, template.Layout } : null,
            quantity = request.Quantity,
            printedAt = DateTime.UtcNow
        });
    }
}

public class PrintLabelRequest
{
    public Guid ProductId { get; set; }
    public Guid? TemplateId { get; set; }
    public string Type { get; set; } = "barcode";
    public int Quantity { get; set; } = 1;
}
