using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.Infrastructure.ExternalServices.Agents;

public class PortfolioBriefingAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;
    public PortfolioBriefingAgent(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
    public string Name => "PortfolioBriefingAgent";
    public string Description => "Resumen y análisis de la cartera del usuario";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        var watchlistRepo = _serviceProvider.GetRequiredService<IWatchlistRepository>();
        var fundRepo = _serviceProvider.GetRequiredService<IFundRepository>();
        var companyRepo = _serviceProvider.GetRequiredService<ICompanyRepository>();
        var metricRepo = _serviceProvider.GetRequiredService<IFinancialMetricRepository>();

        var watchlists = await watchlistRepo.GetAllAsync(ct);
        var funds = await fundRepo.GetTopFundsAsync(200, ct);
        var companies = await companyRepo.GetAllAsync(ct);
        var metrics = await metricRepo.GetAllAsync(ct);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Cartera del usuario:");

        foreach (var w in watchlists)
        {
            sb.AppendLine($"### Watchlist: {w.Name}");
            foreach (var item in w.Items)
            {
                sb.AppendLine($"  - ID: {item.Id}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Fondos en cartera:");
        foreach (var fund in funds.Take(20))
        {
            var latestPerf = fund.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
            sb.AppendLine($"- {fund.Name} (ISIN:{fund.Isin.Value}) Riesgo:{fund.RiskLevel} TER:{fund.ExpenseRatio.Value}%");
            if (latestPerf != null)
                sb.AppendLine($"  Rendimiento 1Y:{latestPerf.Return1Year.Value}% Vol:{latestPerf.Volatility.Value}% Sharpe:{latestPerf.SharpeRatio}");
        }

        sb.AppendLine();
        sb.AppendLine("## Empresas en cartera:");
        foreach (var c in companies.Take(20))
        {
            var companyMetrics = metrics.Where(m => m.EntityType == "Company" && m.EntityId == c.Id).Take(5);
            sb.AppendLine($"- {c.Name} ({c.Ticker}) Sector:{c.Sector} País:{c.Country}");
            foreach (var m in companyMetrics)
                sb.AppendLine($"  {m.MetricName}: {m.Value} {m.Currency} ({m.Year})");
        }

        var systemPrompt = """
            Eres un asesor financiero que prepara un resumen de cartera. Analiza los datos proporcionados y genera:
            
            1. **Resumen de asignación**: Distribución por tipo de activo, sector, geografía
            2. **Rendimiento**: Performance general y por componente
            3. **Riesgos de concentración**: Exposiciones excesivas a un sector/región/activo
            4. **Insights accionables**: Recomendaciones concretas basadas en los datos
            
            Usa SOLO los datos proporcionados. Si faltan datos, indícalo.
            Responde en español con formato Markdown.
            """;

        var userPrompt = string.IsNullOrWhiteSpace(input)
            ? $"Analiza esta cartera:\n\n{sb}"
            : $"Analiza esta cartera enfocándote en: {input}\n\n{sb}";

        var response = await llm.ChatAsync(
            systemPrompt,
            userPrompt,
            new LlmOptions { Temperature = 0.2, MaxTokens = 4000 },
            ct);

        var watchlistCount = watchlists.Count();
        var companiesCount = companies.Count();
        return new AgentOutput { Output = response.Content, Sources = $"{watchlistCount} watchlists, {funds.Count} fondos, {companiesCount} empresas" };
    }
}
