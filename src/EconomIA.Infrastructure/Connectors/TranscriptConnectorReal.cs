using EconomIA.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace EconomIA.Infrastructure.Connectors;

/// <summary>
/// Conector de transcripts de earnings calls: recibe un archivo de texto (.txt)
/// con el transcript y extrae métricas financieras usando LLM.
/// </summary>
public class TranscriptConnectorReal : IDataConnector
{
    private readonly ILlmService _llm;
    private readonly ILogger<TranscriptConnectorReal> _logger;

    public TranscriptConnectorReal(ILlmService llm, ILogger<TranscriptConnectorReal> logger)
    {
        _llm = llm;
        _logger = logger;
    }

    public string ConnectorName => "transcript_connector";
    public string[] SupportedFileTypes => ["txt"];

    public async Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream);
        var transcript = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(transcript))
        {
            _logger.LogWarning("Empty transcript file: {File}", metadata.FileName);
            return [];
        }

        _logger.LogInformation("Processing transcript: {File} ({Chars} chars)", metadata.FileName, transcript.Length);

        // Truncar si es muy largo
        var text = transcript.Length > 15000 ? transcript[..15000] : transcript;

        var systemPrompt = """
            Eres un analista financiero que extrae métricas cuantitativas de transcripts de earnings calls.
            Extrae SOLO datos numéricos concretos mencionados. Para cada métrica devuelve un JSON:
            
            Responde SOLO con un array JSON, sin texto adicional:
            [
              {
                "metric": "Revenue",
                "value": 94.8,
                "period": "Q3 2024",
                "year": 2024,
                "quarter": 3,
                "currency": "USD",
                "confidence": "high",
                "rawText": "fragmento original donde se menciona"
              }
            ]
            
            Métricas típicas: Revenue, Net Income, EPS, Gross Margin, Operating Margin, Free Cash Flow, 
            Guidance Revenue, Guidance EPS, YoY Growth, Users/Subscribers, ARPU, Capex, Debt, Cash.
            Si no hay métricas cuantitativas claras, devuelve [].
            """;

        var response = await _llm.ChatAsync(systemPrompt, $"Transcript de {metadata.EntityName ?? metadata.FileName}:\n\n{text}",
            new LlmOptions { Temperature = 0.1, MaxTokens = 3000 }, ct);

        return AudioConnectorReal.ParseMetricsJson(response.Content, metadata with { Source = metadata.Source ?? "earnings_call" });
    }
}
