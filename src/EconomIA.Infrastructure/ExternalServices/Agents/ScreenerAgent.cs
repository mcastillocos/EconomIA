using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.Infrastructure.ExternalServices.Agents;

public class ScreenerAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;
    public ScreenerAgent(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
    public string Name => "ScreenerAgent";
    public string Description => "Screening inteligente de fondos y empresas con criterios personalizados";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        var fundRepo = _serviceProvider.GetRequiredService<IFundRepository>();
        var companyRepo = _serviceProvider.GetRequiredService<ICompanyRepository>();
        var metricRepo = _serviceProvider.GetRequiredService<IFinancialMetricRepository>();

        var funds = await fundRepo.GetTopFundsAsync(100, ct);
        var companies = await companyRepo.GetAllAsync(ct);
        var metrics = await metricRepo.GetAllAsync(ct);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Fondos disponibles:");
        foreach (var f in funds)
        {
            var latestPerf = f.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
            var return1Y = latestPerf?.Return1Year.Value.ToString("F2") ?? "N/A";
            var sharpe = latestPerf?.SharpeRatio.ToString("F2") ?? "N/A";
            sb.AppendLine($"| {f.Name} | {f.Isin.Value} | {f.Category} | Riesgo:{f.RiskLevel} | TER:{f.ExpenseRatio.Value}% | 1Y:{return1Y}% | Sharpe:{sharpe} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Empresas disponibles:");
        foreach (var c in companies)
        {
            sb.AppendLine($"| {c.Name} | {c.Ticker} | {c.Sector} | {c.Country} |");
        }

        var systemPrompt = """
            Eres un screener financiero inteligente. El usuario te dará criterios de búsqueda y tú debes:
            
            1. Interpretar los criterios del usuario (rentabilidad, riesgo, sector, TER, etc.)
            2. Filtrar los fondos y empresas que cumplan esos criterios usando SOLO los datos proporcionados
            3. Presentar los resultados en una tabla Markdown clara
            4. Para cada resultado, explicar brevemente por qué cumple los criterios
            
            Si los criterios son ambiguos, interprétalos de forma razonable y explica tu interpretación.
            Responde en español con formato Markdown.
            """;

        var response = await llm.ChatAsync(
            systemPrompt,
            $"Criterios del usuario: {input}\n\nDatos disponibles:\n{sb}",
            new LlmOptions { Temperature = 0.1, MaxTokens = 4000 },
            ct);

        var companiesCount = companies.Count();
        return new AgentOutput { Output = response.Content, Sources = $"{funds.Count} fondos, {companiesCount} empresas analizados" };
    }
}
