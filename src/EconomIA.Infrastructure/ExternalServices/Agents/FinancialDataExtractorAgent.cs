using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.Infrastructure.ExternalServices.Agents;

public class FinancialDataExtractorAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;
    public FinancialDataExtractorAgent(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
    public string Name => "FinancialDataExtractorAgent";
    public string Description => "Extracción de datos financieros de CSV/Excel/PDF usando IA";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        var documentRepo = _serviceProvider.GetRequiredService<IDocumentRepository>();
        var companyRepo = _serviceProvider.GetRequiredService<ICompanyRepository>();

        var documents = await documentRepo.GetAllAsync(ct);
        var companies = await companyRepo.GetAllAsync(ct);

        var inputLower = input.ToLowerInvariant();

        var filtered = documents.Where(doc =>
            doc.FileName.Contains(inputLower, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(doc.ExtractedText) && doc.ExtractedText.Contains(inputLower, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(doc => doc.UploadDate)
            .Take(5)
            .ToList();

        if (filtered.Count == 0)
            return new AgentOutput { Output = "No se encontraron documentos que coincidan con la búsqueda.", Sources = null };

        var sb = new System.Text.StringBuilder();
        var sources = new List<string>();

        foreach (var doc in filtered)
        {
            var text = doc.ExtractedText ?? "";
            if (text.Length > 4000) text = text[..4000];
            sb.AppendLine($"## Documento: {doc.FileName}");
            sb.AppendLine($"- Tipo: {doc.FileType}, Fecha: {doc.UploadDate:yyyy-MM-dd}");
            sb.AppendLine($"### Contenido:");
            sb.AppendLine(text);
            sb.AppendLine();
            sources.Add(doc.FileName);
        }

        sb.AppendLine("## Empresas registradas (para asignación):");
        foreach (var c in companies)
        {
            sb.AppendLine($"- {c.Name} ({c.Ticker}) ID:{c.Id}");
        }

        var systemPrompt = """
            Eres un extractor de datos financieros. Analiza los documentos proporcionados y:
            
            1. Identifica TODAS las métricas financieras presentes (ingresos, beneficios, márgenes, ratios, etc.)
            2. Para cada métrica, genera un objeto JSON con estos campos:
               - company: nombre de la empresa (usar las registradas si coincide)
               - metric: nombre de la métrica
               - value: valor numérico
               - currency: moneda (EUR, USD, etc.)
               - year: año
               - quarter: trimestre (null si es anual)
               - confidence: confianza 0-1
               - notes: observaciones
            
            Formato de salida:
            1. **JSON Array** con todas las métricas extraídas
            2. **Resumen en Markdown** con las métricas principales organizadas por empresa
            
            Sé exhaustivo y preciso. Si un dato es ambiguo, indica baja confianza.
            Responde en español (excepto el JSON que usa keys en inglés).
            """;

        var response = await llm.ChatAsync(
            systemPrompt,
            $"Extrae datos financieros de estos documentos:\n\n{sb}",
            new LlmOptions { Temperature = 0.1, MaxTokens = 4000 },
            ct);

        return new AgentOutput { Output = response.Content, Sources = string.Join(", ", sources) };
    }
}
