using EconomIA.Application.Interfaces;
using EconomIA.Domain.Entities;
using EconomIA.Infrastructure.ExternalServices;
using EconomIA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/earnings-calls")]
public class EarningsCallsController : ControllerBase
{
    private readonly EconomIADbContext _db;
    private readonly IAudioTranscriptionService _transcription;
    private readonly ILlmService _llm;
    private readonly IWebHostEnvironment _env;

    public EarningsCallsController(
        EconomIADbContext db,
        IAudioTranscriptionService transcription,
        ILlmService llm,
        IWebHostEnvironment env)
    {
        _db = db;
        _transcription = transcription;
        _llm = llm;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? ticker, [FromQuery] int? year, CancellationToken ct)
    {
        var query = _db.EarningsCalls.AsQueryable();

        if (!string.IsNullOrEmpty(ticker))
            query = query.Where(e => e.Ticker != null && e.Ticker.ToLower() == ticker.ToLower());
        if (year.HasValue)
            query = query.Where(e => e.FiscalYear == year.Value);

        var results = await query.OrderByDescending(e => e.CallDate).Take(50).ToListAsync(ct);

        return Ok(results.Select(e => new
        {
            e.Id,
            e.CompanyName,
            e.Ticker,
            e.FiscalYear,
            e.FiscalQuarter,
            e.CallDate,
            e.Status,
            e.Sentiment,
            e.DurationSeconds,
            e.Language,
            HasTranscript = !string.IsNullOrEmpty(e.TranscriptText),
            HasSummary = !string.IsNullOrEmpty(e.Summary),
            e.CreatedAt,
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var call = await _db.EarningsCalls.FindAsync([id], ct);
        if (call is null) return NotFound();

        return Ok(new
        {
            call.Id,
            call.CompanyName,
            call.Ticker,
            call.CompanyId,
            call.FiscalYear,
            call.FiscalQuarter,
            call.CallDate,
            call.Status,
            call.TranscriptText,
            call.Summary,
            call.Guidance,
            call.KeyMetrics,
            call.Sentiment,
            call.DurationSeconds,
            call.Language,
            call.ErrorMessage,
            call.CreatedAt,
            call.UpdatedAt,
        });
    }

    /// <summary>
    /// Sube un archivo de audio de earnings call. Lo transcribe y analiza automáticamente.
    /// </summary>
    [HttpPost("upload-audio")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100MB para audio
    public async Task<IActionResult> UploadAudio(
        IFormFile file,
        [FromForm] string companyName,
        [FromForm] int fiscalYear,
        [FromForm] int fiscalQuarter,
        [FromForm] string? ticker,
        [FromForm] string? callDate,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No audio file provided.");

        var ext = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
        if (!new[] { "mp3", "wav", "m4a", "mp4", "webm", "ogg", "flac" }.Contains(ext))
            return BadRequest($"Unsupported audio format: {ext}");

        var date = DateTime.TryParse(callDate, out var d) ? d : DateTime.UtcNow;
        var earningsCall = EarningsCall.Create(companyName, fiscalYear, fiscalQuarter, date, ticker);

        // Guardar archivo
        var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads", "earnings");
        Directory.CreateDirectory(uploadsDir);
        var safeFileName = $"{earningsCall.Id}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsDir, safeFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        earningsCall.SetAudioFile(filePath);
        _db.EarningsCalls.Add(earningsCall);
        await _db.SaveChangesAsync(ct);

        // Procesar en background
        _ = Task.Run(() => ProcessEarningsCallAsync(earningsCall.Id), CancellationToken.None);

        return CreatedAtAction(nameof(GetById), new { id = earningsCall.Id }, new { earningsCall.Id, earningsCall.Status });
    }

    /// <summary>
    /// Sube un transcript de texto de una earnings call.
    /// </summary>
    [HttpPost("upload-transcript")]
    public async Task<IActionResult> UploadTranscript(
        [FromBody] UploadTranscriptRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Transcript))
            return BadRequest("Transcript cannot be empty.");

        var date = request.CallDate ?? DateTime.UtcNow;
        var earningsCall = EarningsCall.Create(request.CompanyName, request.FiscalYear, request.FiscalQuarter, date, request.Ticker);
        earningsCall.SetTranscriptDirectly(request.Transcript, request.Language);

        _db.EarningsCalls.Add(earningsCall);
        await _db.SaveChangesAsync(ct);

        // Analizar en background
        _ = Task.Run(() => AnalyzeTranscriptAsync(earningsCall.Id), CancellationToken.None);

        return CreatedAtAction(nameof(GetById), new { id = earningsCall.Id }, new { earningsCall.Id, earningsCall.Status });
    }

    /// <summary>
    /// Re-analiza una earnings call existente.
    /// </summary>
    [HttpPost("{id:guid}/reanalyze")]
    public async Task<IActionResult> Reanalyze(Guid id, CancellationToken ct)
    {
        var call = await _db.EarningsCalls.FindAsync([id], ct);
        if (call is null) return NotFound();
        if (string.IsNullOrEmpty(call.TranscriptText))
            return BadRequest("No transcript available to analyze.");

        _ = Task.Run(() => AnalyzeTranscriptAsync(id), CancellationToken.None);
        return Accepted();
    }

    private async Task ProcessEarningsCallAsync(Guid id)
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EconomIADbContext>();
        var transcription = scope.ServiceProvider.GetRequiredService<IAudioTranscriptionService>();
        var llm = scope.ServiceProvider.GetRequiredService<ILlmService>();

        var call = await db.EarningsCalls.FindAsync(id);
        if (call is null) return;

        try
        {
            // 1. Transcribir
            call.MarkTranscribing();
            await db.SaveChangesAsync();

            await using var audioStream = new FileStream(call.AudioFilePath!, FileMode.Open, FileAccess.Read);
            var result = await transcription.TranscribeAsync(audioStream, Path.GetFileName(call.AudioFilePath!));

            if (!result.Success)
            {
                call.MarkFailed($"Transcription failed: {result.Error}");
                await db.SaveChangesAsync();
                return;
            }

            call.SetTranscript(result.Text, result.Language);
            await db.SaveChangesAsync();

            // 2. Analizar
            await AnalyzeCallInternal(call, llm);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            call.MarkFailed(ex.Message);
            await db.SaveChangesAsync();
        }
    }

    private async Task AnalyzeTranscriptAsync(Guid id)
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EconomIADbContext>();
        var llm = scope.ServiceProvider.GetRequiredService<ILlmService>();

        var call = await db.EarningsCalls.FindAsync(id);
        if (call is null || string.IsNullOrEmpty(call.TranscriptText)) return;

        try
        {
            await AnalyzeCallInternal(call, llm);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            call.MarkFailed(ex.Message);
            await db.SaveChangesAsync();
        }
    }

    private static async Task AnalyzeCallInternal(EarningsCall call, ILlmService llm)
    {
        var transcript = call.TranscriptText!;
        if (transcript.Length > 15000) transcript = transcript[..15000];

        var systemPrompt = """
            Eres un analista financiero experto. Analiza esta earnings call y devuelve un JSON con:
            {
              "summary": "Resumen ejecutivo de 3-5 párrafos de la call",
              "guidance": "Guidance/proyecciones de la empresa para próximos trimestres",
              "keyMetrics": "Métricas clave mencionadas (revenue, EPS, márgenes, etc.) con valores",
              "sentiment": "positive|neutral|negative"
            }
            Responde SOLO con JSON válido, sin texto adicional. En español.
            """;

        var response = await llm.ChatAsync(systemPrompt,
            $"Earnings Call de {call.CompanyName} ({call.Ticker}) - Q{call.FiscalQuarter} {call.FiscalYear}:\n\n{transcript}",
            new LlmOptions { Temperature = 0.2, MaxTokens = 4000 },
            CancellationToken.None);

        // Parse response
        try
        {
            var start = response.Content.IndexOf('{');
            var end = response.Content.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                var json = response.Content[start..(end + 1)];
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                call.SetAnalysis(
                    summary: root.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : response.Content,
                    guidance: root.TryGetProperty("guidance", out var g) ? g.GetString() : null,
                    keyMetrics: root.TryGetProperty("keyMetrics", out var k) ? k.GetString() : null,
                    sentiment: root.TryGetProperty("sentiment", out var sent) ? sent.GetString() : null
                );
            }
            else
            {
                call.SetAnalysis(response.Content, null, null, null);
            }
        }
        catch
        {
            call.SetAnalysis(response.Content, null, null, null);
        }
    }
}

public record UploadTranscriptRequest
{
    public string CompanyName { get; init; } = string.Empty;
    public string? Ticker { get; init; }
    public int FiscalYear { get; init; }
    public int FiscalQuarter { get; init; }
    public DateTime? CallDate { get; init; }
    public string Transcript { get; init; } = string.Empty;
    public string? Language { get; init; }
}
