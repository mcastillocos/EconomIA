using EconomIA.Domain.Entities;
using EconomIA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly EconomIADbContext _db;

    public AlertsController(EconomIADbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var alerts = await _db.Set<Alert>().AsNoTracking()
            .OrderByDescending(a => a.CreatedAt).ToListAsync();
        return Ok(alerts);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAlertRequest req)
    {
        var alert = Alert.Create(req.Name, req.EntityType, req.EntityId, req.Field, req.Operator, req.Threshold);
        _db.Set<Alert>().Add(alert);
        await _db.SaveChangesAsync();
        return Ok(alert);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var alert = await _db.Set<Alert>().FindAsync(id);
        if (alert == null) return NotFound();
        _db.Set<Alert>().Remove(alert);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}/toggle")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var alert = await _db.Set<Alert>().FindAsync(id);
        if (alert == null) return NotFound();
        if (alert.IsActive) alert.Deactivate(); else alert.Activate();
        await _db.SaveChangesAsync();
        return Ok(alert);
    }

    [HttpPut("{id}/reset")]
    public async Task<IActionResult> Reset(Guid id)
    {
        var alert = await _db.Set<Alert>().FindAsync(id);
        if (alert == null) return NotFound();
        alert.Reset();
        await _db.SaveChangesAsync();
        return Ok(alert);
    }
}

public record CreateAlertRequest(
    string Name,
    string EntityType,
    Guid? EntityId,
    string Field,
    string Operator,
    decimal Threshold
);
