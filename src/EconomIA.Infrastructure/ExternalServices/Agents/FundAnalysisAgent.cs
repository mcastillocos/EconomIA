using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.Infrastructure.ExternalServices.Agents;

public class FundAnalysisAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;
    public FundAnalysisAgent(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
    public string Name => "FundAnalysisAgent";
    public string Description => "Análisis completo de un fondo de inversión con datos disponibles";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        var fundRepo = _serviceProvider.GetRequiredService<IFundRepository>();
        var metricRepo = _serviceProvider.GetRequiredService<IFinancialMetricRepository>();

        var funds = await fundRepo.GetTopFundsAsync(200, ct);
        var inputLower = input.ToLowerInvariant();

        var matched = funds.Where(f =>
            f.Name.Contains(inputLower, StringComparison.OrdinalIgnoreCase) ||
            f.Isin.Value.Contains(inputLower, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matched.Count == 0)
            return new AgentOutput { Output = "No se encontraron fondos que coincidan con la búsqueda.", Sources = null };

        var sb = new System.Text.StringBuilder();
        var sources = new List<string>();

        foreach (var fund in matched.Take(3))
        {
            sb.AppendLine($"## Fondo: {fund.Name}");
            sb.AppendLine($"- ISIN: {fund.Isin.Value}");
            sb.AppendLine($"- Nivel de riesgo: {fund.RiskLevel}");
            sb.AppendLine($"- NAV: {fund.NetAssetValue.Amount} {fund.NetAssetValue.Currency}");
            sb.AppendLine($"- TER: {fund.ExpenseRatio.Value}%");

            var latestPerf = fund.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
            if (latestPerf != null)
            {
                sb.AppendLine("### Rendimiento más reciente:");
                sb.AppendLine($"  - 1M: {latestPerf.Return1Month.Value}%, 3M: {latestPerf.Return3Months.Value}%, 6M: {latestPerf.Return6Months.Value}%");
                sb.AppendLine($"  - 1Y: {latestPerf.Return1Year.Value}%, 3Y: {latestPerf.Return3Years.Value}%, 5Y: {latestPerf.Return5Years.Value}%");
                sb.AppendLine($"  - Volatilidad: {latestPerf.Volatility.Value}%, Sharpe: {latestPerf.SharpeRatio}");
            }

            var metrics = await metricRepo.GetByEntityAsync("Fund", fund.Id, ct);
            if (metrics.Any())
            {
                sb.AppendLine("### Métricas adicionales:");
                foreach (var m in metrics.Take(20))
                    sb.AppendLine($"  - {m.MetricName}: {m.Value} {m.Currency} (Año:{m.Year} Q:{m.Quarter})");
            }
            sb.AppendLine();
            sources.Add($"{fund.Name} ({fund.Isin.Value})");
        }

        var systemPrompt = """
            Eres un analista de fondos de inversión experto. Realiza un análisis completo usando SOLO los datos proporcionados.
            
            Checklist de análisis (10 puntos):
            1. Rentabilidades (absoluta y relativa a categoría)
            2. Volatilidad y ratio de Sharpe
            3. Máximo drawdown estimado
            4. TER y costes
            5. Categoría y estilo de inversión
            6. Composición y diversificación
            7. Top holdings (si disponible)
            8. Consistencia del gestor
            9. Alternativas potenciales
            10. Puntos de vigilancia
            
            Responde en español con formato Markdown estructurado.
            """;

        var response = await llm.ChatAsync(
            systemPrompt,
            $"Analiza los siguientes fondos:\n\n{sb}",
            new LlmOptions { Temperature = 0.2, MaxTokens = 4000 },
            ct);

        return new AgentOutput { Output = response.Content, Sources = string.Join(", ", sources) };
    }
}
