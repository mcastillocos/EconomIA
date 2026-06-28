using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.Infrastructure.ExternalServices.Agents;

public class EarningsCallAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;
    public EarningsCallAgent(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
    public string Name => "EarningsCallAgent";
    public string Description => "Destilación de earnings calls: guidance, sorpresas, cambios de tono";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        var documentRepo = _serviceProvider.GetRequiredService<IDocumentRepository>();
        var companyRepo = _serviceProvider.GetRequiredService<ICompanyRepository>();

        var documents = await documentRepo.GetAllAsync(ct);
        var companies = await companyRepo.GetAllAsync(ct);

        var inputLower = input.ToLowerInvariant();
        string[] earningsKeywords = ["earning", "transcript", "call"];

        var filtered = documents.Where(doc =>
            earningsKeywords.Any(k => doc.FileName.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
            doc.FileName.Contains(inputLower, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(doc.ExtractedText) && doc.ExtractedText.Contains(inputLower, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(doc => doc.UploadDate)
            .Take(3)
            .ToList();

        if (filtered.Count == 0)
            return new AgentOutput { Output = "No se encontraron documentos de earnings calls que coincidan con la búsqueda.", Sources = null };

        var sb = new System.Text.StringBuilder();
        var sources = new List<string>();

        foreach (var doc in filtered)
        {
            var text = doc.ExtractedText ?? "";
            if (text.Length > 3000) text = text[..3000];
            sb.AppendLine($"## Documento: {doc.FileName}");
            sb.AppendLine($"- Tipo: {doc.FileType}, Fecha: {doc.UploadDate:yyyy-MM-dd}");
            sb.AppendLine($"### Contenido extraído:");
            sb.AppendLine(text);
            sb.AppendLine();
            sources.Add(doc.FileName);
        }

        var systemPrompt = """
            Eres un analista especializado en earnings calls. Analiza los documentos proporcionados siguiendo este checklist:
            
            1. **Guidance**: Proyecciones de la empresa para próximos trimestres/año
            2. **Sorpresas vs consenso**: Métricas que superaron o no alcanzaron expectativas
            3. **Cambios de tono**: Diferencias en el lenguaje respecto a calls anteriores
            4. **Segmentos clave**: Performance por línea de negocio
            5. **Asignación de capital**: Inversiones, buybacks, dividendos, M&A
            6. **Riesgos mencionados**: Factores de riesgo destacados por la directiva
            7. **Preguntas de analistas**: Temas más preguntados y respuestas clave
            8. **Keywords**: Palabras y frases repetidas que indican prioridades
            
            Usa SOLO la información del documento proporcionado.
            Responde en español con formato Markdown.
            """;

        var response = await llm.ChatAsync(
            systemPrompt,
            $"Analiza estas earnings calls:\n\n{sb}",
            new LlmOptions { Temperature = 0.2, MaxTokens = 4000 },
            ct);

        return new AgentOutput { Output = response.Content, Sources = string.Join(", ", sources) };
    }
}
