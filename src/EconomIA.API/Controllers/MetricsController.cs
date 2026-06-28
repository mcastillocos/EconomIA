using EconomIA.Application.DTOs;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using Microsoft.AspNetCore.Mvc;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly IFinancialMetricRepository _repository;

    public MetricsController(IFinancialMetricRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FinancialMetricDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFiltered(
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] string? metricName,
        [FromQuery] int? year,
        [FromQuery] int? quarter,
        [FromQuery] string? source,
        [FromQuery] bool? validated,
        CancellationToken ct)
    {
        var metrics = await _repository.GetFilteredAsync(entityType, entityId, metricName, year, quarter, source, validated, ct);
        return Ok(metrics.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FinancialMetricDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var metric = await _repository.GetByIdAsync(id, ct);
        return metric is null ? NotFound() : Ok(MapToDto(metric));
    }

    [HttpPost("{id:guid}/validate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Validate(Guid id, CancellationToken ct)
    {
        var metric = await _repository.GetByIdAsync(id, ct);
        if (metric is null) return NotFound();

        metric.MarkValidated();
        await _repository.UpdateAsync(metric, ct);
        await _repository.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/unvalidate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unvalidate(Guid id, CancellationToken ct)
    {
        var metric = await _repository.GetByIdAsync(id, ct);
        if (metric is null) return NotFound();

        metric.MarkUnvalidated();
        await _repository.UpdateAsync(metric, ct);
        await _repository.SaveChangesAsync(ct);

        return NoContent();
    }

    private static FinancialMetricDto MapToDto(FinancialMetric m) => new(
        m.Id, m.EntityType, m.EntityId, m.Ticker, m.Isin,
        m.MetricName, m.Value, m.Period, m.Year, m.Quarter,
        m.Currency, m.Source, m.SourceType, m.FileName,
        m.Page, m.Row, m.Url, m.Confidence, m.RawText,
        m.Validated, m.ValidatedAt, m.CreatedAt);
}
