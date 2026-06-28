using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.Infrastructure.ExternalServices.Agents;

public class RiskAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;
    public RiskAgent(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
    public string Name => "RiskAgent";
    public string Description => "Evaluación de riesgos: fondos, empresas y carteras";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        var fundRepo = _serviceProvider.GetRequiredService<IFundRepository>();
        var companyRepo = _serviceProvider.GetRequiredService<ICompanyRepository>();
        var metricRepo = _serviceProvider.GetRequiredService<IFinancialMetricRepository>();

        var funds = await fundRepo.GetTopFundsAsync(100, ct);
        var companies = await companyRepo.GetAllAsync(ct);
        var metrics = await metricRepo.GetAllAsync(ct);

        var inputLower = input.ToLowerInvariant();

        var matchedFunds = funds.Where(f =>
            f.Name.Contains(inputLower, StringComparison.OrdinalIgnoreCase) ||
            f.Isin.Value.Contains(inputLower, StringComparison.OrdinalIgnoreCase) ||
            (f.Category != null && f.Category.Contains(inputLower, StringComparison.OrdinalIgnoreCase)))
            .Take(10)
            .ToList();

        var matchedCompanies = companies.Where(c =>
            c.Name.Contains(inputLower, StringComparison.OrdinalIgnoreCase) ||
            c.Ticker.Contains(inputLower, StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();

        var sb = new System.Text.StringBuilder();
        var sources = new List<string>();

        if (matchedFunds.Count > 0)
        {
            sb.AppendLine("## Fondos analizados:");
            foreach (var fund in matchedFunds)
            {
                var latestPerf = fund.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
                sb.AppendLine($"- {fund.Name} (ISIN:{fund.Isin.Value})");
                sb.AppendLine($"  Categoría:{fund.Category} Riesgo:{fund.RiskLevel} TER:{fund.ExpenseRatio.Value}%");
                if (latestPerf != null)
                    sb.AppendLine($"  1Y:{latestPerf.Return1Year.Value}% Vol:{latestPerf.Volatility.Value}% Sharpe:{latestPerf.SharpeRatio}");
                sources.Add(fund.Name);
            }
        }

        if (matchedCompanies.Count > 0)
        {
            sb.AppendLine("## Empresas analizadas:");
            foreach (var company in matchedCompanies)
            {
                sb.AppendLine($"- {company.Name} ({company.Ticker}) Sector:{company.Sector} País:{company.Country}");
                var companyMetrics = metrics.Where(m => m.EntityType == "Company" && m.EntityId == company.Id).Take(15);
                foreach (var m in companyMetrics)
                    sb.AppendLine($"  {m.MetricName}: {m.Value} {m.Currency} ({m.Year})");
                sources.Add($"{company.Name} ({company.Ticker})");
            }
        }

        if (matchedFunds.Count == 0 && matchedCompanies.Count == 0)
        {
            sb.AppendLine("## Análisis general de riesgo (sin filtro específico):");
            foreach (var fund in funds.Take(10))
            {
                var latestPerf = fund.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
                sb.AppendLine($"- {fund.Name} Riesgo:{fund.RiskLevel} TER:{fund.ExpenseRatio.Value}%");
                if (latestPerf != null)
                    sb.AppendLine($"  Vol:{latestPerf.Volatility.Value}% Sharpe:{latestPerf.SharpeRatio}");
            }
            foreach (var c in companies.Take(5))
                sb.AppendLine($"- {c.Name} ({c.Ticker}) Sector:{c.Sector}");
            sources.Add("Cartera general");
        }

        var systemPrompt = """
            Eres un analista de riesgos financieros. Evalúa los activos proporcionados usando este framework:
            
            **Tipos de riesgo a evaluar:**
            - Riesgo de mercado: Sensibilidad a movimientos generales del mercado
            - Riesgo de concentración: Exposición excesiva a un factor
            - Riesgo de liquidez: Facilidad de venta sin impacto en precio
            - Riesgo de crédito: Probabilidad de impago o deterioro
            - Riesgo operacional: Problemas internos de gestión
            - Riesgo macro: Sensibilidad a tipos de interés, inflación, divisa
            
            **Clasificación:**
            - 🟢 Bajo (1-2): Riesgo contenido, dentro de parámetros normales
            - 🟡 Medio-bajo (3): Algunos factores a vigilar
            - 🟠 Medio (4-5): Riesgo significativo, requiere monitorización
            - 🔴 Alto (6-7): Riesgo elevado, acción recomendada
            
            **Formato de salida:**
            1. **Perfil global de riesgo**: Puntuación general y resumen
            2. **Tabla de riesgos**: Cada tipo con su clasificación y justificación
            3. **Escenarios**: Mejor caso / Caso base / Peor caso
            4. **Red flags**: Señales de alerta identificadas
            5. **Recomendaciones de mitigación**: Acciones concretas
            6. **Datos faltantes**: Información necesaria para un análisis más preciso
            
            Usa SOLO los datos proporcionados. Responde en español con formato Markdown.
            """;

        var response = await llm.ChatAsync(
            systemPrompt,
            $"Evalúa el riesgo de: {input}\n\nDatos disponibles:\n{sb}",
            new LlmOptions { Temperature = 0.2, MaxTokens = 4000 },
            ct);

        return new AgentOutput { Output = response.Content, Sources = string.Join(", ", sources) };
    }
}
