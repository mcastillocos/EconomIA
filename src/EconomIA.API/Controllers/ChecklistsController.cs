using EconomIA.Domain.Entities;
using EconomIA.Infrastructure.Connectors;
using EconomIA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChecklistsController : ControllerBase
{
    private readonly EconomIADbContext _db;

    public ChecklistsController(EconomIADbContext db)
    {
        _db = db;
    }

    // ─── Templates ───

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates(CancellationToken ct)
    {
        var templates = await _db.ChecklistTemplates
            .Include(t => t.Items)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        return Ok(templates.Select(t => new
        {
            t.Id, t.Name, t.Description, t.Category, t.IsBuiltIn,
            ItemCount = t.Items.Count,
            Sections = t.Items.Select(i => i.Section).Distinct(),
        }));
    }

    [HttpGet("templates/{id:guid}")]
    public async Task<IActionResult> GetTemplate(Guid id, CancellationToken ct)
    {
        var template = await _db.ChecklistTemplates
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (template is null) return NotFound();
        return Ok(template);
    }

    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request, CancellationToken ct)
    {
        var template = ChecklistTemplate.Create(request.Name, request.Category, request.Description);

        foreach (var item in request.Items)
            template.AddItem(item.Text, item.Section, item.Order, item.ItemType ?? "boolean", item.HelpText);

        _db.ChecklistTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
    }

    [HttpPost("templates/seed")]
    public async Task<IActionResult> SeedPredefined(CancellationToken ct)
    {
        var existing = await _db.ChecklistTemplates.Where(t => t.IsBuiltIn).CountAsync(ct);
        if (existing > 0)
            return Ok(new { message = "Templates predefinidos ya existen", count = existing });

        foreach (var template in PredefinedChecklists.All)
            _db.ChecklistTemplates.Add(template);

        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Templates predefinidos creados", count = PredefinedChecklists.All.Count });
    }

    [HttpDelete("templates/{id:guid}")]
    public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken ct)
    {
        var template = await _db.ChecklistTemplates.FindAsync([id], ct);
        if (template is null) return NotFound();
        if (template.IsBuiltIn) return BadRequest("No se pueden eliminar templates predefinidos");

        _db.ChecklistTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ─── Instances ───

    [HttpGet("instances")]
    public async Task<IActionResult> GetInstances([FromQuery] string? entityName = null, CancellationToken ct = default)
    {
        var query = _db.ChecklistInstances
            .Include(i => i.Answers)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(i => i.EntityName.Contains(entityName));

        var instances = await query.OrderByDescending(i => i.UpdatedAt).Take(50).ToListAsync(ct);

        return Ok(instances.Select(i => new
        {
            i.Id, i.TemplateId, i.EntityType, i.EntityName, i.Status,
            i.CreatedAt, i.UpdatedAt, i.Notes,
            AnswerCount = i.Answers.Count,
        }));
    }

    [HttpGet("instances/{id:guid}")]
    public async Task<IActionResult> GetInstance(Guid id, CancellationToken ct)
    {
        var instance = await _db.ChecklistInstances
            .Include(i => i.Answers)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (instance is null) return NotFound();
        return Ok(instance);
    }

    [HttpPost("instances")]
    public async Task<IActionResult> CreateInstance([FromBody] CreateInstanceRequest request, CancellationToken ct)
    {
        var template = await _db.ChecklistTemplates.FindAsync([request.TemplateId], ct);
        if (template is null) return BadRequest("Template no encontrado");

        var instance = ChecklistInstance.Create(request.TemplateId, request.EntityType, request.EntityName, request.EntityId);
        _db.ChecklistInstances.Add(instance);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetInstance), new { id = instance.Id }, instance);
    }

    [HttpPut("instances/{id:guid}/answer")]
    public async Task<IActionResult> SetAnswer(Guid id, [FromBody] SetAnswerRequest request, CancellationToken ct)
    {
        var instance = await _db.ChecklistInstances
            .Include(i => i.Answers)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (instance is null) return NotFound();

        instance.SetAnswer(request.TemplateItemId, request.Value, request.Comment);
        await _db.SaveChangesAsync(ct);
        return Ok(instance);
    }

    [HttpPut("instances/{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteRequest? request = null, CancellationToken ct = default)
    {
        var instance = await _db.ChecklistInstances.FindAsync([id], ct);
        if (instance is null) return NotFound();

        instance.MarkCompleted(request?.Notes);
        await _db.SaveChangesAsync(ct);
        return Ok(instance);
    }

    [HttpGet("predefined")]
    public IActionResult GetPredefinedTemplates()
    {
        return Ok(PredefinedChecklists.All.Select(t => new
        {
            t.Name, t.Description, t.Category,
            ItemCount = t.Items.Count,
            Sections = t.Items.Select(i => i.Section).Distinct(),
        }));
    }
}

public record CreateTemplateRequest
{
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<CreateTemplateItemRequest> Items { get; init; } = [];
}

public record CreateTemplateItemRequest
{
    public string Text { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty;
    public int Order { get; init; }
    public string? ItemType { get; init; }
    public string? HelpText { get; init; }
}

public record CreateInstanceRequest
{
    public Guid TemplateId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string EntityName { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }
}

public record SetAnswerRequest
{
    public Guid TemplateItemId { get; init; }
    public string Value { get; init; } = string.Empty;
    public string? Comment { get; init; }
}

public record CompleteRequest
{
    public string? Notes { get; init; }
}
