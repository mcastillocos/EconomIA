using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.Infrastructure.ExternalServices.Agents;

/// <summary>
/// Genera un briefing diario basado en las posiciones del usuario (watchlists + empresas).
/// </summary>
public class DailyNewsAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;

    public DailyNewsAgent(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Name => "DailyNewsAgent";
    public string Description => "Briefing diario de noticias relevantes sobre posiciones y sectores";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var companyRepo = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();
        var watchlistRepo = scope.ServiceProvider.GetRequiredService<IWatchlistRepository>();
        var fundRepo = scope.ServiceProvider.GetRequiredService<IFundRepository>();

        // Recoger contexto del usuario
        var companies = await companyRepo.GetAllAsync(ct);
        var watchlists = await watchlistRepo.GetAllAsync(ct);
        var topFunds = await fundRepo.GetTopFundsAsync(10, ct);

        var dataContext = "POSICIONES Y SEGUIMIENTO DEL USUARIO:\n\n";
        var sources = new List<string>();

        if (companies.Count > 0)
        {
            dataContext += "EMPRESAS EN CARTERA:\n";
            foreach (var c in companies)
            {
                dataContext += $"- {c.Name} ({c.Ticker ?? "sin ticker"}) | Sector: {c.Sector ?? "?"} | País: {c.Country ?? "?"}\n";
            }
            sources.Add($"Empresas registradas: {companies.Count}");
        }

        if (watchlists.Count > 0)
        {
            dataContext += "\nWATCHLISTS:\n";
            foreach (var w in watchlists)
            {
                dataContext += $"- {w.Name}: {w.Items.Count} items\n";
            }
            sources.Add($"Watchlists: {watchlists.Count}");
        }

        if (topFunds.Count > 0)
        {
            dataContext += "\nFONDOS TOP MONITORIZADOS:\n";
            foreach (var f in topFunds.Take(5))
            {
                dataContext += $"- {f.Name} ({f.Isin.Value}) | Cat: {f.Category}\n";
            }
            sources.Add("Top fondos EconomIA");
        }

        // Tema específico si el usuario lo indica
        var topicFilter = !string.IsNullOrWhiteSpace(input) ? $"\nFOCO ESPECÍFICO: {input}\n" : "";

        var systemPrompt = @"Eres el agente de briefing diario de economIA. Tu trabajo es generar un resumen conciso de lo que el usuario debería saber hoy sobre sus posiciones.

REGLAS:
- Genera el briefing basándote en las posiciones del usuario proporcionadas.
- Estructura: Resumen ejecutivo (3 líneas) → Sección por empresa/fondo relevante → Calendario próximo → Riesgos a vigilar.
- NO inventes noticias. Si no tienes datos de noticias reales, indica que el sistema aún no tiene feed de noticias conectado y genera un briefing basado en la información disponible.
- Sé conciso y práctico. No repitas datos ya conocidos.
- Marca claramente qué es dato verificado vs inferencia.
- NO recomiendes compra/venta.

FORMATO: Markdown con secciones claras. Máximo 800 palabras.
Responde en español.";

        var userPrompt = $"Genera el briefing diario para hoy ({DateTime.UtcNow:yyyy-MM-dd}).\n\n{dataContext}{topicFilter}";

        var response = await llm.ChatAsync(systemPrompt, userPrompt, new LlmOptions { MaxTokens = 3000, Temperature = 0.3 }, ct);

        return new AgentOutput
        {
            Output = response.Content,
            Sources = sources.Count > 0 ? string.Join("\n", sources) : "Sin datos de posiciones"
        };
    }
}

/// <summary>
/// Screener inteligente: filtra empresas y fondos según criterios del usuario.
/// </summary>
public class ScreenerAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;

    public ScreenerAgent(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Name => "ScreenerAgent";
    public string Description => "Screening inteligente de fondos y empresas por criterios cuantitativos y cualitativos";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var companyRepo = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();
        var fundRepo = scope.ServiceProvider.GetRequiredService<IFundRepository>();
        var metricRepo = scope.ServiceProvider.GetRequiredService<IFinancialMetricRepository>();

        var companies = await companyRepo.GetAllAsync(ct);
        var funds = await fundRepo.GetTopFundsAsync(100, ct);
        var metrics = await metricRepo.GetAllAsync(ct);

        var dataContext = "UNIVERSO DISPONIBLE:\n\n";
        var sources = new List<string>();

        // Empresas con métricas
        if (companies.Count > 0)
        {
            dataContext += $"EMPRESAS ({companies.Count}):\n";
            foreach (var c in companies)
            {
                var companyMetrics = metrics.Where(m => m.EntityType == "company" && m.EntityId == c.Id).ToList();
                dataContext += $"- {c.Name} ({c.Ticker ?? "?"}) | Sector: {c.Sector ?? "?"} | País: {c.Country ?? "?"}";
                if (companyMetrics.Count > 0)
                {
                    var keyMetrics = companyMetrics
                        .GroupBy(m => m.MetricName)
                        .Select(g => g.OrderByDescending(m => m.Year).First())
                        .Take(5);
                    dataContext += " | Métricas: " + string.Join(", ", keyMetrics.Select(m => $"{m.MetricName}={m.Value}"));
                }
                dataContext += "\n";
            }
            sources.Add($"Empresas: {companies.Count}");
        }

        // Fondos
        if (funds.Count > 0)
        {
            dataContext += $"\nFONDOS ({funds.Count}):\n";
            foreach (var f in funds.Take(30))
            {
                var perf = f.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
                dataContext += $"- #{f.RankingPosition} {f.Name} | Cat: {f.Category} | TER: {f.ExpenseRatio.Value}% | Riesgo: {f.RiskLevel}";
                if (perf is not null)
                    dataContext += $" | 1Y: {perf.Return1Year.Value}% | Sharpe: {perf.SharpeRatio}";
                dataContext += "\n";
            }
            sources.Add($"Fondos: {funds.Count}");
        }

        if (companies.Count == 0 && funds.Count == 0)
        {
            dataContext += "No hay empresas ni fondos cargados. Sube datos primero.\n";
        }

        var systemPrompt = @"Eres el agente de screening de economIA. Filtras el universo de empresas y fondos según los criterios del usuario.

REGLAS:
- Aplica los criterios indicados por el usuario sobre los datos disponibles.
- Si un criterio no puede evaluarse por falta de datos, indícalo claramente.
- Ordena resultados por relevancia al criterio.
- Para cada resultado, explica por qué cumple (o no) cada criterio.
- Si no hay suficientes datos, sugiere qué información falta para hacer un screening completo.
- NO inventes datos.
- NO recomiendes compra/venta.

FORMATO:
1. Criterios aplicados (interpretación de la petición)
2. Resultados que cumplen (con justificación)
3. Resultados parciales (cumplen algunos criterios)
4. Datos faltantes para mejorar el screening

Responde en español. Formato Markdown.";

        var userPrompt = $"CRITERIOS DE SCREENING: {input}\n\nDATOS DISPONIBLES:\n{dataContext}";

        var response = await llm.ChatAsync(systemPrompt, userPrompt, new LlmOptions { MaxTokens = 3500, Temperature = 0.2 }, ct);

        return new AgentOutput
        {
            Output = response.Content,
            Sources = sources.Count > 0 ? string.Join("\n", sources) : "Sin datos disponibles"
        };
    }
}

/// <summary>
/// Resumen del estado de la cartera del usuario.
/// </summary>
public class PortfolioBriefingAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;

    public PortfolioBriefingAgent(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Name => "PortfolioBriefingAgent";
    public string Description => "Resumen y estado de la cartera del usuario";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var companyRepo = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();
        var watchlistRepo = scope.ServiceProvider.GetRequiredService<IWatchlistRepository>();
        var metricRepo = scope.ServiceProvider.GetRequiredService<IFinancialMetricRepository>();
        var fundRepo = scope.ServiceProvider.GetRequiredService<IFundRepository>();

        var companies = await companyRepo.GetAllAsync(ct);
        var watchlists = await watchlistRepo.GetAllAsync(ct);
        var metrics = await metricRepo.GetAllAsync(ct);
        var funds = await fundRepo.GetTopFundsAsync(20, ct);

        var dataContext = "ESTADO DE LA CARTERA:\n\n";
        var sources = new List<string>();

        // Empresas con sus métricas
        if (companies.Count > 0)
        {
            dataContext += $"EMPRESAS ({companies.Count}):\n";
            foreach (var c in companies)
            {
                var companyMetrics = metrics
                    .Where(m => m.EntityType == "company" && m.EntityId == c.Id)
                    .OrderByDescending(m => m.Year)
                    .ThenByDescending(m => m.Quarter)
                    .Take(10)
                    .ToList();

                dataContext += $"\n  {c.Name} ({c.Ticker ?? "?"}) — {c.Sector ?? "sin sector"}, {c.Country ?? "?"}\n";
                if (companyMetrics.Count > 0)
                {
                    foreach (var m in companyMetrics)
                        dataContext += $"    · {m.MetricName}: {m.Value}{(m.Currency is not null ? $" {m.Currency}" : "")} ({m.Year}{(m.Quarter.HasValue ? $"/Q{m.Quarter}" : "")})\n";
                }
                else
                {
                    dataContext += "    · Sin métricas cargadas\n";
                }
            }
            sources.Add($"Empresas en cartera: {companies.Count}");
        }

        // Fondos en watchlists
        if (watchlists.Count > 0)
        {
            dataContext += $"\nWATCHLISTS ({watchlists.Count}):\n";
            foreach (var w in watchlists)
            {
                dataContext += $"  {w.Name} ({w.Items.Count} items)\n";
            }
            sources.Add($"Watchlists: {watchlists.Count}");
        }

        // Top funds performance
        if (funds.Count > 0)
        {
            dataContext += "\nFONDOS REFERENCIA (Top 5):\n";
            foreach (var f in funds.Take(5))
            {
                var perf = f.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
                dataContext += $"  #{f.RankingPosition} {f.Name} | VL: {f.NetAssetValue.Amount} {f.NetAssetValue.Currency}";
                if (perf is not null)
                    dataContext += $" | 1Y: {perf.Return1Year.Value}%";
                dataContext += "\n";
            }
            sources.Add("Fondos Top EconomIA");
        }

        var systemPrompt = @"Eres el agente de resumen de cartera de economIA. Generas un informe del estado actual de las posiciones del usuario.

REGLAS:
- Usa SOLO datos proporcionados. NUNCA inventes cifras.
- Estructura: Estado general → Por posición → Concentración sectorial → Puntos de atención
- Si faltan datos (ej: sin precios actuales), indícalo.
- NO recomiendes compra/venta.
- Sé conciso y práctico.

Responde en español. Formato Markdown.";

        var userPrompt = $"Genera el resumen de cartera.\n\n{dataContext}";
        if (!string.IsNullOrWhiteSpace(input))
            userPrompt += $"\n\nFOCO: {input}";

        var response = await llm.ChatAsync(systemPrompt, userPrompt, new LlmOptions { MaxTokens = 3000, Temperature = 0.2 }, ct);

        return new AgentOutput
        {
            Output = response.Content,
            Sources = sources.Count > 0 ? string.Join("\n", sources) : "Sin datos de cartera"
        };
    }
}
