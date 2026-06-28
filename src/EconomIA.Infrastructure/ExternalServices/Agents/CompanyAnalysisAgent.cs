using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.Infrastructure.ExternalServices.Agents;

public class CompanyAnalysisAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;
    public CompanyAnalysisAgent(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
    public string Name => "CompanyAnalysisAgent";
    public string Description => "Análisis fundamental completo de una empresa con datos disponibles";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        var companyRepo = _serviceProvider.GetRequiredService<ICompanyRepository>();
        var metricRepo = _serviceProvider.GetRequiredService<IFinancialMetricRepository>();

        var companies = await companyRepo.GetAllAsync(ct);
        var inputLower = input.ToLowerInvariant();

        var matched = companies.Where(c =>
            c.Name.Contains(inputLower, StringComparison.OrdinalIgnoreCase) ||
            c.Ticker.Contains(inputLower, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matched.Count == 0)
            return new AgentOutput { Output = "No se encontraron empresas que coincidan con la búsqueda.", Sources = null };

        var sb = new System.Text.StringBuilder();
        var sources = new List<string>();

        foreach (var company in matched.Take(3))
        {
            var metrics = await metricRepo.GetByEntityAsync("Company", company.Id, ct);
            sb.AppendLine($"## Empresa: {company.Name} ({company.Ticker})");
            sb.AppendLine($"- Sector: {company.Sector}, País: {company.Country}, Industria: {company.Industry}");
            sb.AppendLine($"- ISIN: {company.Isin}");
            if (!string.IsNullOrEmpty(company.Notes)) sb.AppendLine($"- Notas: {company.Notes}");
            sb.AppendLine("### Métricas:");
            foreach (var m in metrics.Take(30))
            {
                sb.AppendLine($"  - {m.MetricName}: {m.Value} {m.Currency} (Año:{m.Year} Q:{m.Quarter}, Fuente:{m.Source}, Confianza:{m.Confidence})");
            }
            sb.AppendLine();
            sources.Add($"{company.Name} ({company.Ticker})");
        }

        var systemPrompt = """
            Eres un analista financiero experto. Realiza un análisis fundamental completo de la empresa usando SOLO los datos proporcionados.
            
            Checklist de análisis (10 puntos):
            1. Modelo de negocio y ventajas competitivas
            2. Crecimiento (ingresos, beneficios)
            3. Márgenes (bruto, operativo, neto)
            4. Deuda y estructura de capital
            5. Generación de caja (FCF)
            6. Rentabilidad (ROIC, ROE)
            7. Valoración (múltiplos si disponibles)
            8. Riesgos principales
            9. Guidance y perspectivas
            10. Puntos de vigilancia
            
            Responde en español con formato Markdown estructurado.
            """;

        var response = await llm.ChatAsync(
            systemPrompt,
            $"Analiza la siguiente empresa:\n\n{sb}",
            new LlmOptions { Temperature = 0.2, MaxTokens = 4000 },
            ct);

        return new AgentOutput { Output = response.Content, Sources = string.Join(", ", sources) };
    }
}
