using EconomIA.Application.Interfaces;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using EconomIA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/screener")]
public class LearningScreenerController : ControllerBase
{
    private readonly EconomIADbContext _db;
    private readonly ILlmService _llm;
    private readonly IFundRepository _fundRepo;
    private readonly ICompanyRepository _companyRepo;

    public LearningScreenerController(
        EconomIADbContext db,
        ILlmService llm,
        IFundRepository fundRepo,
        ICompanyRepository companyRepo)
    {
        _db = db;
        _llm = llm;
        _fundRepo = fundRepo;
        _companyRepo = companyRepo;
    }

    /// <summary>Obtener el perfil del inversor (crea uno si no existe)</summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var profile = await _db.InvestorProfiles.FirstOrDefaultAsync(p => p.UserId == "default", ct);
        if (profile is null)
        {
            profile = InvestorProfile.Create("default");
            _db.InvestorProfiles.Add(profile);
            await _db.SaveChangesAsync(ct);
        }
        return Ok(profile);
    }

    /// <summary>Actualizar perfil desde cuestionario</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req, CancellationToken ct)
    {
        var profile = await _db.InvestorProfiles.FirstOrDefaultAsync(p => p.UserId == "default", ct);
        if (profile is null)
        {
            profile = InvestorProfile.Create("default");
            _db.InvestorProfiles.Add(profile);
        }

        profile.UpdateFromQuestionnaire(
            req.RiskTolerance,
            req.InvestmentHorizon,
            req.InvestmentStyle,
            req.AssetPreference,
            req.MaxExpenseRatio,
            req.MinReturn1Y,
            req.EsgPreference,
            JsonSerializer.Serialize(req.PreferredSectors ?? []),
            JsonSerializer.Serialize(req.PreferredGeographies ?? []),
            JsonSerializer.Serialize(req.ExcludedSectors ?? [])
        );

        await _db.SaveChangesAsync(ct);
        return Ok(profile);
    }

    /// <summary>Obtener recomendaciones activas</summary>
    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations([FromQuery] string? status, CancellationToken ct)
    {
        var query = _db.ScreenerRecommendations.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);
        else
            query = query.Where(r => r.Status == "active");

        var results = await query.OrderByDescending(r => r.Score).Take(20).ToListAsync(ct);
        return Ok(results);
    }

    /// <summary>Generar nuevas recomendaciones basadas en el perfil</summary>
    [HttpPost("recommendations/generate")]
    public async Task<IActionResult> GenerateRecommendations(CancellationToken ct)
    {
        var profile = await _db.InvestorProfiles.FirstOrDefaultAsync(p => p.UserId == "default", ct);
        if (profile is null)
            return BadRequest("No investor profile found. Complete the questionnaire first.");

        var funds = await _fundRepo.GetTopFundsAsync(100, ct);
        var companies = await _companyRepo.GetAllAsync(ct);

        // Construir contexto de datos para el LLM
        var dataContext = new System.Text.StringBuilder();
        dataContext.AppendLine("## Fondos:");
        foreach (var f in funds)
        {
            var perf = f.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
            dataContext.AppendLine($"- {f.Name} | ISIN:{f.Isin.Value} | Cat:{f.Category} | Riesgo:{f.RiskLevel} | TER:{f.ExpenseRatio.Value}% | 1Y:{perf?.Return1Year.Value.ToString("F1") ?? "N/A"}% | Sharpe:{perf?.SharpeRatio.ToString("F2") ?? "N/A"}");
        }
        dataContext.AppendLine("\n## Empresas:");
        foreach (var c in companies)
        {
            dataContext.AppendLine($"- {c.Name} | {c.Ticker} | {c.Sector}/{c.Industry} | {c.Country} | {c.Market}");
        }

        var systemPrompt = $$"""
            Eres un asesor financiero que genera recomendaciones personalizadas.
            
            PERFIL DEL INVERSOR:
            - Tolerancia al riesgo: {{profile.RiskTolerance}}
            - Horizonte: {{profile.InvestmentHorizon}}
            - Estilo: {{profile.InvestmentStyle}}
            - Preferencia activos: {{profile.AssetPreference}}
            - TER máximo: {{profile.MaxExpenseRatio}}%
            - Rentabilidad mínima 1Y: {{profile.MinReturn1Y}}%
            - ESG: {{(profile.EsgPreference ? "Sí" : "No")}}
            - Sectores preferidos: {{profile.PreferredSectors}}
            - Geografías: {{profile.PreferredGeographies}}
            - Sectores excluidos: {{profile.ExcludedSectors}}
            
            Genera EXACTAMENTE un array JSON con 5-10 recomendaciones del universo disponible.
            Cada recomendación:
            {
              "entityType": "fund" o "company",
              "entityName": "nombre exacto del dato",
              "ticker": "ticker si aplica",
              "isin": "ISIN si es fondo",
              "reason": "explicación breve de por qué encaja con el perfil",
              "score": 0-100 (compatibilidad con perfil),
              "category": "core|tactical|speculative",
              "metrics": "{\"return1Y\": X, \"ter\": Y}"
            }
            
            Responde SOLO con el array JSON, sin texto adicional.
            Prioriza compatibilidad con el perfil. Score alto = muy compatible.
            """;

        var response = await _llm.ChatAsync(systemPrompt, dataContext.ToString(),
            new LlmOptions { Temperature = 0.2, MaxTokens = 4000 }, ct);

        // Parse y guardar recomendaciones
        var recommendations = ParseRecommendations(response.Content, profile.Id);

        if (recommendations.Count > 0)
        {
            // Desactivar recomendaciones antiguas
            var oldActive = await _db.ScreenerRecommendations
                .Where(r => r.ProfileId == profile.Id && r.Status == "active")
                .ToListAsync(ct);
            foreach (var old in oldActive)
                old.Dismiss();

            _db.ScreenerRecommendations.AddRange(recommendations);
            profile.RecordInteraction();
            await _db.SaveChangesAsync(ct);
        }

        return Ok(recommendations);
    }

    /// <summary>Descartar una recomendación</summary>
    [HttpPut("recommendations/{id:guid}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid id, CancellationToken ct)
    {
        var rec = await _db.ScreenerRecommendations.FindAsync([id], ct);
        if (rec is null) return NotFound();
        rec.Dismiss();
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    /// <summary>Guardar una recomendación</summary>
    [HttpPut("recommendations/{id:guid}/save")]
    public async Task<IActionResult> Save(Guid id, CancellationToken ct)
    {
        var rec = await _db.ScreenerRecommendations.FindAsync([id], ct);
        if (rec is null) return NotFound();
        rec.Save();
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    /// <summary>Marcar como invertido</summary>
    [HttpPut("recommendations/{id:guid}/invest")]
    public async Task<IActionResult> Invest(Guid id, CancellationToken ct)
    {
        var rec = await _db.ScreenerRecommendations.FindAsync([id], ct);
        if (rec is null) return NotFound();
        rec.MarkInvested();
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    private static List<ScreenerRecommendation> ParseRecommendations(string json, Guid profileId)
    {
        var results = new List<ScreenerRecommendation>();
        try
        {
            var start = json.IndexOf('[');
            var end = json.LastIndexOf(']');
            if (start < 0 || end < 0) return results;

            using var doc = JsonDocument.Parse(json[start..(end + 1)]);
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var entityName = item.TryGetProperty("entityName", out var n) ? n.GetString() ?? "" : "";
                var score = item.TryGetProperty("score", out var s) ? s.GetDecimal() : 50;
                if (string.IsNullOrEmpty(entityName)) continue;

                results.Add(ScreenerRecommendation.Create(
                    profileId,
                    item.TryGetProperty("entityType", out var et) ? et.GetString() ?? "fund" : "fund",
                    entityName,
                    item.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "",
                    Math.Clamp(score, 0, 100),
                    item.TryGetProperty("category", out var c) ? c.GetString() ?? "tactical" : "tactical",
                    item.TryGetProperty("ticker", out var t) ? t.GetString() : null,
                    item.TryGetProperty("isin", out var i) ? i.GetString() : null,
                    item.TryGetProperty("metrics", out var m) ? m.ToString() : null
                ));
            }
        }
        catch { }
        return results;
    }
}

public record UpdateProfileRequest
{
    public string RiskTolerance { get; init; } = "moderate";
    public string InvestmentHorizon { get; init; } = "medium";
    public string InvestmentStyle { get; init; } = "blend";
    public string AssetPreference { get; init; } = "both";
    public decimal MaxExpenseRatio { get; init; } = 1.5m;
    public decimal MinReturn1Y { get; init; } = 0m;
    public bool EsgPreference { get; init; }
    public string[]? PreferredSectors { get; init; }
    public string[]? PreferredGeographies { get; init; }
    public string[]? ExcludedSectors { get; init; }
}
