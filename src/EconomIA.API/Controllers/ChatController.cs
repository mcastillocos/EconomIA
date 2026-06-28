using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using Microsoft.AspNetCore.Mvc;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILlmService _llm;
    private readonly ICompanyRepository _companyRepo;
    private readonly IFundRepository _fundRepo;
    private readonly IFinancialMetricRepository _metricRepo;

    public ChatController(
        ILlmService llm,
        ICompanyRepository companyRepo,
        IFundRepository fundRepo,
        IFinancialMetricRepository metricRepo)
    {
        _llm = llm;
        _companyRepo = companyRepo;
        _fundRepo = fundRepo;
        _metricRepo = metricRepo;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message cannot be empty.");

        // Build context from available data
        var context = await BuildContextAsync(request.Message, ct);

        var systemPrompt = @"Eres el asistente financiero de economIA. Respondes preguntas sobre fondos, empresas, carteras y datos financieros.

REGLAS ESTRICTAS (ANTI-ALUCINACIÓN):
- Solo responde con datos que se te proporcionan en el contexto.
- Si no tienes información suficiente, responde: ""No tengo datos suficientes para responder a eso.""
- NUNCA inventes cifras, nombres de fondos o empresas.
- NUNCA recomiendes compra o venta.
- Cita la fuente de cada dato que menciones.
- Diferencia claramente entre dato verificado e inferencia.
- Si un dato tiene confianza 'low', adviértelo al usuario.

DISCLAIMER: economIA es una herramienta de apoyo al análisis financiero. No constituye recomendación de inversión.

Responde en español. Sé conciso y preciso.";

        var userPrompt = $"CONTEXTO DISPONIBLE:\n{context}\n\nPREGUNTA DEL USUARIO:\n{request.Message}";

        var response = await _llm.ChatAsync(systemPrompt, userPrompt, new LlmOptions
        {
            MaxTokens = request.MaxTokens ?? 2000,
            Temperature = 0.3
        }, ct);

        return Ok(new ChatResponse
        {
            Message = response.Content,
            Provider = response.Provider,
            Model = response.Model,
            TokensUsed = response.TotalTokens
        });
    }

    [HttpPost("agent")]
    [ProducesResponseType(typeof(AgentRunResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunAgent([FromBody] RunAgentRequest request, [FromServices] IAgentService agentService, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.AgentName) || string.IsNullOrWhiteSpace(request.Input))
            return BadRequest("AgentName and Input are required.");

        var result = await agentService.RunAgentAsync(request.AgentName, request.Input, request.Context, ct);

        return Ok(new AgentRunResponse
        {
            RunId = result.RunId,
            AgentName = result.AgentName,
            Status = result.Status,
            Output = result.Output,
            Sources = result.Sources,
            Error = result.Error
        });
    }

    private async Task<string> BuildContextAsync(string query, CancellationToken ct)
    {
        var contextParts = new List<string>();

        // Load companies
        var companies = await _companyRepo.GetAllAsync(ct);
        if (companies.Count > 0)
        {
            contextParts.Add($"EMPRESAS REGISTRADAS ({companies.Count}):");
            foreach (var c in companies.Take(20))
            {
                contextParts.Add($"  - {c.Name} ({c.Ticker ?? "sin ticker"}) | Sector: {c.Sector ?? "?"} | País: {c.Country ?? "?"}");
            }
        }

        // Load funds summary
        var funds = await _fundRepo.GetTopFundsAsync(20, ct);
        if (funds.Count > 0)
        {
            contextParts.Add($"\nFONDOS TOP ({funds.Count}):");
            foreach (var f in funds.Take(10))
            {
                contextParts.Add($"  - #{f.RankingPosition} {f.Name} (ISIN: {f.Isin.Value}) | Cat: {f.Category} | VL: {f.NetAssetValue.Amount} {f.NetAssetValue.Currency} | TER: {f.ExpenseRatio.Value}%");
            }
        }

        // Search for relevant metrics
        var metrics = await _metricRepo.GetAllAsync(ct);
        if (metrics.Count > 0)
        {
            var relevant = metrics.Take(30).ToList();
            contextParts.Add($"\nDATOS FINANCIEROS RECIENTES ({metrics.Count} total, mostrando {relevant.Count}):");
            foreach (var m in relevant)
            {
                var source = m.Source ?? m.FileName ?? "manual";
                contextParts.Add($"  - {m.MetricName}: {m.Value}{(m.Currency is not null ? $" {m.Currency}" : "")} | Entidad: {m.EntityType} | Periodo: {m.Year}{(m.Quarter.HasValue ? $"/Q{m.Quarter}" : "")} | Fuente: {source} | Confianza: {m.Confidence}");
            }
        }

        if (contextParts.Count == 0)
            contextParts.Add("No hay datos cargados en el sistema todavía. El usuario puede subir archivos CSV/Excel/PDF en la sección Uploads.");

        return string.Join("\n", contextParts);
    }
}

public record ChatRequest
{
    public string Message { get; init; } = string.Empty;
    public int? MaxTokens { get; init; }
}

public record ChatResponse
{
    public string Message { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int TokensUsed { get; init; }
}

public record RunAgentRequest
{
    public string AgentName { get; init; } = string.Empty;
    public string Input { get; init; } = string.Empty;
    public Dictionary<string, string>? Context { get; init; }
}

public record AgentRunResponse
{
    public Guid RunId { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Output { get; init; } = string.Empty;
    public string? Sources { get; init; }
    public string? Error { get; init; }
}
