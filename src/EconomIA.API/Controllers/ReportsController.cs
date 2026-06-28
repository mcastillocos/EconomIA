using EconomIA.Application.Interfaces;
using EconomIA.Domain.Entities;
using EconomIA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly EconomIADbContext _db;
    private readonly IAgentService _agentService;

    public ReportsController(EconomIADbContext db, IAgentService agentService)
    {
        _db = db;
        _agentService = agentService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReportSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var reports = await _db.AIReports
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReportSummaryDto
            {
                Id = r.Id,
                EntityType = r.EntityType,
                ReportType = r.ReportType,
                Title = r.Title,
                Confidence = r.Confidence,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(reports);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReportDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var report = await _db.AIReports.FindAsync([id], ct);
        if (report is null) return NotFound();

        return Ok(new ReportDetailDto
        {
            Id = report.Id,
            EntityType = report.EntityType,
            EntityId = report.EntityId,
            ReportType = report.ReportType,
            Title = report.Title,
            Content = report.Content,
            Sources = report.Sources,
            Confidence = report.Confidence,
            CreatedAt = report.CreatedAt
        });
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(ReportDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Generate([FromBody] GenerateReportRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
            return BadRequest("Input is required.");

        var agentName = request.ReportType switch
        {
            "company" => "CompanyAnalysisAgent",
            "fund" => "FundAnalysisAgent",
            _ => "CompanyAnalysisAgent"
        };

        var result = await _agentService.RunAgentAsync(agentName, request.Input, null, ct);

        if (result.Status == "failed")
            return StatusCode(500, new { error = result.Error });

        var report = AIReport.Create(
            entityType: request.ReportType ?? "company",
            entityId: null,
            reportType: $"{agentName} Analysis",
            title: $"Análisis: {request.Input}",
            content: result.Output,
            sources: result.Sources,
            confidence: "medium"
        );

        _db.AIReports.Add(report);
        await _db.SaveChangesAsync(ct);

        return Ok(new ReportDetailDto
        {
            Id = report.Id,
            EntityType = report.EntityType,
            EntityId = report.EntityId,
            ReportType = report.ReportType,
            Title = report.Title,
            Content = report.Content,
            Sources = report.Sources,
            Confidence = report.Confidence,
            CreatedAt = report.CreatedAt
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var report = await _db.AIReports.FindAsync([id], ct);
        if (report is null) return NotFound();

        _db.AIReports.Remove(report);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record ReportSummaryDto
{
    public Guid Id { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string ReportType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Confidence { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ReportDetailDto
{
    public Guid Id { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }
    public string ReportType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Content { get; init; }
    public string? Sources { get; init; }
    public string? Confidence { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record GenerateReportRequest
{
    public string Input { get; init; } = string.Empty;
    public string? ReportType { get; init; }
}
