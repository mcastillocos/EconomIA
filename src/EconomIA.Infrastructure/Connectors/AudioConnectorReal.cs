using EconomIA.Application.Interfaces;
using EconomIA.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;

namespace EconomIA.Infrastructure.Connectors;

/// <summary>
/// Conector de audio real: transcribe el audio con Whisper y luego extrae métricas
/// financieras del transcript usando LLM.
/// </summary>
public class AudioConnectorReal : IDataConnector
{
    private readonly IAudioTranscriptionService _transcription;
    private readonly ILlmService _llm;
    private readonly ILogger<AudioConnectorReal> _logger;

    public AudioConnectorReal(IAudioTranscriptionService transcription, ILlmService llm, ILogger<AudioConnectorReal> logger)
    {
        _transcription = transcription;
        _llm = llm;
        _logger = logger;
    }

    public string ConnectorName => "audio_connector";
    public string[] SupportedFileTypes => ["mp3", "wav", "m4a"];

    public async Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        // 1. Transcribir audio
        var result = await _transcription.TranscribeAsync(stream, metadata.FileName, ct: ct);
        if (!result.Success)
        {
            _logger.LogWarning("Audio transcription failed for {File}: {Error}", metadata.FileName, result.Error);
            return [];
        }

        _logger.LogInformation("Audio transcribed: {Chars} chars from {File}", result.Text.Length, metadata.FileName);

        // 2. Extraer métricas del transcript usando LLM
        return await ExtractMetricsFromTranscript(result.Text, metadata, ct);
    }

    internal async Task<IReadOnlyList<NormalizedDataPoint>> ExtractMetricsFromTranscript(string transcript, ConnectorMetadata metadata, CancellationToken ct)
    {
        // Truncar transcript si es muy largo (límite de tokens)
        var text = transcript.Length > 12000 ? transcript[..12000] : transcript;

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
                "rawText": "fragmento original"
              }
            ]
            
            Métricas típicas: Revenue, Net Income, EPS, Gross Margin, Operating Margin, Free Cash Flow, 
            Guidance Revenue, Guidance EPS, YoY Growth, Users/Subscribers, ARPU, Capex.
            Si no hay métricas cuantitativas claras, devuelve [].
            """;

        var response = await _llm.ChatAsync(systemPrompt, $"Transcript de {metadata.EntityName ?? metadata.FileName}:\n\n{text}",
            new LlmOptions { Temperature = 0.1, MaxTokens = 3000 }, ct);

        return ParseMetricsJson(response.Content, metadata);
    }

    public static IReadOnlyList<NormalizedDataPoint> ParseMetricsJson(string json, ConnectorMetadata metadata)
    {
        var results = new List<NormalizedDataPoint>();
        try
        {
            // Extraer el array JSON del response
            var start = json.IndexOf('[');
            var end = json.LastIndexOf(']');
            if (start < 0 || end < 0) return results;

            var arrayJson = json[start..(end + 1)];
            using var doc = System.Text.Json.JsonDocument.Parse(arrayJson);

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var metric = item.TryGetProperty("metric", out var m) ? m.GetString() ?? "" : "";
                var value = item.TryGetProperty("value", out var v) ? v.GetDecimal() : 0;
                if (string.IsNullOrEmpty(metric)) continue;

                results.Add(new NormalizedDataPoint
                {
                    Source = metadata.Source ?? "earnings_call",
                    SourceType = "audio",
                    EntityType = metadata.EntityType ?? "company",
                    EntityName = metadata.EntityName ?? "",
                    Ticker = metadata.Ticker,
                    Metric = metric,
                    Value = value,
                    Period = item.TryGetProperty("period", out var p) ? p.GetString() : null,
                    Year = item.TryGetProperty("year", out var y) ? y.GetInt32() : null,
                    Quarter = item.TryGetProperty("quarter", out var q) ? q.GetInt32() : null,
                    Currency = item.TryGetProperty("currency", out var c) ? c.GetString() : null,
                    Confidence = item.TryGetProperty("confidence", out var conf) ? conf.GetString() ?? "medium" : "medium",
                    RawText = item.TryGetProperty("rawText", out var rt) ? rt.GetString() : null,
                    FileName = metadata.FileName,
                });
            }
        }
        catch { /* Si falla el parsing, devolver vacío */ }

        return results;
    }
}
