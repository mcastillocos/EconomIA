using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.Infrastructure.ExternalServices.Agents;

public class DataValidationAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;
    public DataValidationAgent(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
    public string Name => "DataValidationAgent";
    public string Description => "Validación automática de datos financieros: anomalías, inconsistencias, outliers";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        var metricRepo = _serviceProvider.GetRequiredService<IFinancialMetricRepository>();
        var companyRepo = _serviceProvider.GetRequiredService<ICompanyRepository>();

        var allMetrics = await metricRepo.GetAllAsync(ct);
        var companies = await companyRepo.GetAllAsync(ct);

        var inputLower = input.ToLowerInvariant();

        var matchedCompany = companies.FirstOrDefault(c =>
            c.Name.Contains(inputLower, StringComparison.OrdinalIgnoreCase) ||
            c.Ticker.Contains(inputLower, StringComparison.OrdinalIgnoreCase));

        var metricsToValidate = matchedCompany != null
            ? allMetrics.Where(m => m.EntityId == matchedCompany.Id).ToList()
            : allMetrics.ToList();

        var sb = new System.Text.StringBuilder();
        var grouped = metricsToValidate
            .GroupBy(m => new { m.EntityType, m.EntityId })
            .Take(10);

        foreach (var group in grouped)
        {
            var entity = companies.FirstOrDefault(c => c.Id == group.Key.EntityId);
            var entityName = entity != null ? $"{entity.Name} ({entity.Ticker})" : $"{group.Key.EntityType}:{group.Key.EntityId}";
            sb.AppendLine($"## Entidad: {entityName}");

            foreach (var m in group.Take(30))
            {
                sb.AppendLine($"  - {m.MetricName}: {m.Value} {m.Currency} | Año:{m.Year} Q:{m.Quarter} | Fuente:{m.Source} | Confianza:{m.Confidence} | Validado:{m.Validated}");
            }
            sb.AppendLine();
        }

        var systemPrompt = """
            Eres un auditor de datos financieros. Analiza los datos proporcionados buscando:
            
            - Problemas de coherencia (márgenes imposibles, ratios sin sentido)
            - Outliers (valores que se desvían significativamente de la serie temporal)
            - Inconsistencias temporales (saltos inexplicables entre periodos)
            - Problemas de unidades (mezcla de monedas, porcentajes vs absolutos)
            - Duplicados (misma métrica reportada dos veces con valores distintos)
            
            Formato de salida:
            1. **Resumen**: Estado general de calidad de datos
            2. **Errores críticos**: Datos claramente incorrectos
            3. **Advertencias**: Datos sospechosos que requieren revisión
            4. **Datos faltantes**: Métricas esperadas que no aparecen
            5. **Recomendaciones**: Acciones para mejorar la calidad
            
            Responde en español con formato Markdown.
            """;

        var response = await llm.ChatAsync(
            systemPrompt,
            $"Valida estos datos financieros:\n\n{sb}",
            new LlmOptions { Temperature = 0.1, MaxTokens = 3500 },
            ct);

        var source = matchedCompany != null
            ? $"{matchedCompany.Name} ({matchedCompany.Ticker})"
            : $"{metricsToValidate.Count} métricas analizadas";
        return new AgentOutput { Output = response.Content, Sources = source };
    }
}
