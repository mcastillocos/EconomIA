using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.Infrastructure.ExternalServices.Agents;

public class AnnualReportReaderAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;
    public AnnualReportReaderAgent(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
    public string Name => "AnnualReportReaderAgent";
    public string Description => "Lectura de informes anuales con checklist estructurado";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        var documentRepo = _serviceProvider.GetRequiredService<IDocumentRepository>();
        var companyRepo = _serviceProvider.GetRequiredService<ICompanyRepository>();

        var documents = await documentRepo.GetAllAsync(ct);
        var companies = await companyRepo.GetAllAsync(ct);

        var inputLower = input.ToLowerInvariant();
        string[] reportKeywords = ["annual", "informe", "report", "memoria"];

        var filtered = documents.Where(doc =>
            reportKeywords.Any(k => doc.FileName.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
            doc.FileName.Contains(inputLower, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(doc.ExtractedText) && doc.ExtractedText.Contains(inputLower, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(doc => doc.UploadDate)
            .Take(3)
            .ToList();

        if (filtered.Count == 0)
            return new AgentOutput { Output = "No se encontraron informes anuales que coincidan con la búsqueda.", Sources = null };

        var sb = new System.Text.StringBuilder();
        var sources = new List<string>();

        foreach (var doc in filtered)
        {
            var text = doc.ExtractedText ?? "";
            if (text.Length > 4000) text = text[..4000];
            sb.AppendLine($"## Documento: {doc.FileName}");
            sb.AppendLine($"- Tipo: {doc.FileType}, Fecha: {doc.UploadDate:yyyy-MM-dd}");
            sb.AppendLine($"### Contenido extraído:");
            sb.AppendLine(text);
            sb.AppendLine();
            sources.Add(doc.FileName);
        }

        var systemPrompt = """
            Eres un analista especializado en lectura de informes anuales. Analiza los documentos usando este checklist:
            
            1. **Evolución de ingresos**: Tendencia y drivers de crecimiento
            2. **Márgenes**: Bruto, operativo, neto y su evolución
            3. **Deuda**: Nivel de endeudamiento, vencimientos, covenants
            4. **Cash flow**: Generación de caja operativa y libre
            5. **Estrategia**: Planes estratégicos y ejecución
            6. **Riesgos**: Factores de riesgo identificados
            7. **ESG**: Compromisos medioambientales, sociales y de gobernanza
            8. **Dividendos**: Política de retribución al accionista
            9. **Outlook**: Perspectivas y guidance para el próximo ejercicio
            
            Usa SOLO la información del documento proporcionado.
            Responde en español con formato Markdown.
            """;

        var response = await llm.ChatAsync(
            systemPrompt,
            $"Analiza estos informes anuales:\n\n{sb}",
            new LlmOptions { Temperature = 0.2, MaxTokens = 4000 },
            ct);

        return new AgentOutput { Output = response.Content, Sources = string.Join(", ", sources) };
    }
}
