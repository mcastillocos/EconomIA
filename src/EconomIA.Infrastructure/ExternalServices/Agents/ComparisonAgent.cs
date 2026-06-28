using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.Infrastructure.ExternalServices.Agents;

public class ComparisonAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;
    public ComparisonAgent(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
    public string Name => "ComparisonAgent";
    public string Description => "Comparativa entre fondos y/o empresas";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        var companyRepo = _serviceProvider.GetRequiredService<ICompanyRepository>();
        var fundRepo = _serviceProvider.GetRequiredService<IFundRepository>();
        var metricRepo = _serviceProvider.GetRequiredService<IFinancialMetricRepository>();

        var companies = await companyRepo.GetAllAsync(ct);
        var funds = await fundRepo.GetTopFundsAsync(100, ct);
        var metrics = await metricRepo.GetAllAsync(ct);

        string[] separators = [" vs ", " VS ", " contra ", ",", " y "];
        var terms = input.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var sb = new System.Text.StringBuilder();
        var sources = new List<string>();

        foreach (var term in terms)
        {
            var termLower = term.ToLowerInvariant();
            sb.AppendLine($"## Elemento: {term}");

            var company = companies.FirstOrDefault(c =>
                c.Name.Contains(termLower, StringComparison.OrdinalIgnoreCase) ||
                c.Ticker.Contains(termLower, StringComparison.OrdinalIgnoreCase));

            if (company != null)
            {
                sb.AppendLine($"**Tipo: Empresa**");
                sb.AppendLine($"- Nombre: {company.Name}, Ticker: {company.Ticker}");
                sb.AppendLine($"- Sector: {company.Sector}, País: {company.Country}, Industria: {company.Industry}");

                var companyMetrics = metrics.Where(m => m.EntityType == "Company" && m.EntityId == company.Id).Take(20);
                foreach (var m in companyMetrics)
                    sb.AppendLine($"  - {m.MetricName}: {m.Value} {m.Currency} (Año:{m.Year} Q:{m.Quarter})");

                sources.Add($"{company.Name} ({company.Ticker})");
            }
            else
            {
                var fund = funds.FirstOrDefault(f =>
                    f.Name.Contains(termLower, StringComparison.OrdinalIgnoreCase) ||
                    f.Isin.Value.Contains(termLower, StringComparison.OrdinalIgnoreCase));

                if (fund != null)
                {
                    sb.AppendLine($"**Tipo: Fondo**");
                    sb.AppendLine($"- Nombre: {fund.Name}, ISIN: {fund.Isin.Value}");
                    sb.AppendLine($"- Riesgo: {fund.RiskLevel}, TER: {fund.ExpenseRatio.Value}%");

                    var latestPerf = fund.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
                    if (latestPerf != null)
                    {
                        sb.AppendLine($"  - 1M:{latestPerf.Return1Month.Value}% 3M:{latestPerf.Return3Months.Value}% 6M:{latestPerf.Return6Months.Value}%");
                        sb.AppendLine($"  - 1Y:{latestPerf.Return1Year.Value}% 3Y:{latestPerf.Return3Years.Value}% 5Y:{latestPerf.Return5Years.Value}%");
                        sb.AppendLine($"  - Volatilidad:{latestPerf.Volatility.Value}% Sharpe:{latestPerf.SharpeRatio}");
                    }

                    sources.Add($"{fund.Name} ({fund.Isin.Value})");
                }
                else
                {
                    sb.AppendLine($"*No se encontró como empresa ni como fondo*");
                }
            }
            sb.AppendLine();
        }

        var systemPrompt = """
            Eres un analista financiero comparativo. Compara objetivamente los elementos proporcionados usando SOLO los datos disponibles.
            
            Estructura tu respuesta:
            1. **Tabla comparativa**: Métricas clave lado a lado
            2. **Pros y contras de cada uno**: Ventajas y desventajas relativas
            3. **Conclusión**: Para qué perfil de inversor es mejor cada opción
            
            No inventes datos. Si falta información, indícalo claramente.
            Responde en español con formato Markdown.
            """;

        var response = await llm.ChatAsync(
            systemPrompt,
            $"Compara los siguientes elementos:\n\n{sb}",
            new LlmOptions { Temperature = 0.2, MaxTokens = 3500 },
            ct);

        return new AgentOutput { Output = response.Content, Sources = string.Join(" vs ", sources) };
    }
}
