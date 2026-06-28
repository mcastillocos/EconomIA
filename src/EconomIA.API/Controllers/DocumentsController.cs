using EconomIA.Application.DTOs;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using EconomIA.Infrastructure.Connectors;
using Microsoft.AspNetCore.Mvc;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFinancialMetricRepository _metricRepository;
    private readonly IEnumerable<IDataConnector> _connectors;
    private readonly IWebHostEnvironment _env;

    public DocumentsController(
        IDocumentRepository documentRepository,
        IFinancialMetricRepository metricRepository,
        IEnumerable<IDataConnector> connectors,
        IWebHostEnvironment env)
    {
        _documentRepository = documentRepository;
        _metricRepository = metricRepository;
        _connectors = connectors;
        _env = env;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var docs = await _documentRepository.GetAllAsync(ct);
        return Ok(docs.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var doc = await _documentRepository.GetByIdAsync(id, ct);
        return doc is null ? NotFound() : Ok(MapToDto(doc));
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB max
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string entityType,
        [FromForm] Guid? entityId,
        [FromForm] string? source,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        var fileType = GetFileType(file.FileName);
        if (fileType is null)
            return BadRequest($"Unsupported file type: {Path.GetExtension(file.FileName)}");

        // Save file to disk
        var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsDir, safeFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        var document = UploadedDocument.Create(entityType, entityId, file.FileName, fileType, filePath, file.Length, source);
        await _documentRepository.AddAsync(document, ct);
        await _documentRepository.SaveChangesAsync(ct);

        // Process document asynchronously (extract data)
        _ = Task.Run(async () => await ProcessDocumentAsync(document.Id), CancellationToken.None);

        return CreatedAtAction(nameof(GetById), new { id = document.Id }, MapToDto(document));
    }

    [HttpPost("{id:guid}/reprocess")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reprocess(Guid id, CancellationToken ct)
    {
        var doc = await _documentRepository.GetByIdAsync(id, ct);
        if (doc is null) return NotFound();

        _ = Task.Run(async () => await ProcessDocumentAsync(id), CancellationToken.None);
        return Accepted();
    }

    private async Task ProcessDocumentAsync(Guid documentId)
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var docRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var metricRepo = scope.ServiceProvider.GetRequiredService<IFinancialMetricRepository>();
        var connectors = scope.ServiceProvider.GetRequiredService<IEnumerable<IDataConnector>>();

        var doc = await docRepo.GetByIdAsync(documentId);
        if (doc is null) return;

        doc.MarkProcessing();
        await docRepo.UpdateAsync(doc);
        await docRepo.SaveChangesAsync();

        try
        {
            var connector = connectors.FirstOrDefault(c => c.SupportedFileTypes.Contains(doc.FileType));
            if (connector is null)
            {
                doc.MarkCompleted(null, "No connector available for this file type.", null);
                await docRepo.UpdateAsync(doc);
                await docRepo.SaveChangesAsync();
                return;
            }

            await using var stream = new FileStream(doc.FilePath, FileMode.Open, FileAccess.Read);
            var metadata = new ConnectorMetadata
            {
                FileName = doc.FileName,
                EntityType = doc.EntityType,
                Source = doc.Source
            };

            var dataPoints = await connector.ExtractAsync(stream, metadata);

            // Convert to FinancialMetric entities
            var metrics = dataPoints.Select(dp => FinancialMetric.Create(
                dp.EntityType,
                doc.EntityId,
                dp.Metric,
                dp.Value,
                dp.Ticker,
                dp.Isin,
                dp.Period,
                dp.Year,
                dp.Quarter,
                dp.Currency,
                dp.Source,
                dp.SourceType,
                dp.FileName,
                dp.Page,
                dp.Row,
                dp.Url,
                dp.Confidence,
                dp.RawText
            )).ToList();

            if (metrics.Count > 0)
            {
                await metricRepo.AddRangeAsync(metrics);
                await metricRepo.SaveChangesAsync();
            }

            doc.MarkCompleted(null, $"Extracted {metrics.Count} metrics.", null);
        }
        catch (Exception ex)
        {
            doc.MarkFailed(ex.Message);
        }

        await docRepo.UpdateAsync(doc);
        await docRepo.SaveChangesAsync();
    }

    private static string? GetFileType(string fileName)
    {
        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "csv" or "tsv" => "csv",
            "xlsx" or "xls" => "excel",
            "pdf" => "pdf",
            "txt" => "transcript",
            "mp3" or "wav" or "m4a" => "audio",
            _ => null
        };
    }

    private static DocumentDto MapToDto(UploadedDocument d) => new(
        d.Id, d.EntityType, d.EntityId, d.FileName, d.FileType,
        d.Source, d.FilePath, d.FileSize, d.UploadDate,
        d.Status, d.Summary, d.ErrorMessage);
}
