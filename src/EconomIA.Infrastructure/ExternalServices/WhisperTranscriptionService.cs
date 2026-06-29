using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EconomIA.Infrastructure.ExternalServices;

/// <summary>
/// Servicio de transcripción de audio usando OpenAI Whisper API.
/// Soporta mp3, wav, m4a, mp4, webm, mpeg, mpga, oga, ogg, flac.
/// </summary>
public interface IAudioTranscriptionService
{
    Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string fileName, string? language = null, CancellationToken ct = default);
}

public record TranscriptionResult
{
    public string Text { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public double? DurationSeconds { get; init; }
    public bool Success { get; init; }
    public string? Error { get; init; }
}

public class WhisperTranscriptionService : IAudioTranscriptionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhisperTranscriptionService> _logger;
    private readonly string _model;

    public WhisperTranscriptionService(HttpClient httpClient, IConfiguration config, ILogger<WhisperTranscriptionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _model = config["OpenAI:WhisperModel"] ?? "whisper-1";

        var apiKey = config["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public async Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string fileName, string? language = null, CancellationToken ct = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();

            var streamContent = new StreamContent(audioStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(fileName));
            content.Add(streamContent, "file", fileName);
            content.Add(new StringContent(_model), "model");
            content.Add(new StringContent("verbose_json"), "response_format");

            if (!string.IsNullOrEmpty(language))
                content.Add(new StringContent(language), "language");

            _logger.LogInformation("Transcribing audio file: {FileName}", fileName);

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/transcriptions", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Whisper API error {Status}: {Body}", response.StatusCode, errorBody);
                return new TranscriptionResult { Success = false, Error = $"API error {response.StatusCode}: {errorBody}" };
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var text = root.GetProperty("text").GetString() ?? "";
            var lang = root.TryGetProperty("language", out var langProp) ? langProp.GetString() ?? "" : "";
            var duration = root.TryGetProperty("duration", out var durProp) ? durProp.GetDouble() : (double?)null;

            _logger.LogInformation("Transcription complete: {Length} chars, {Duration}s, language={Lang}", text.Length, duration, lang);

            return new TranscriptionResult
            {
                Text = text,
                Language = lang,
                DurationSeconds = duration,
                Success = true,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transcribe audio: {FileName}", fileName);
            return new TranscriptionResult { Success = false, Error = ex.Message };
        }
    }

    private static string GetMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "mp3" => "audio/mpeg",
            "wav" => "audio/wav",
            "m4a" => "audio/mp4",
            "mp4" => "audio/mp4",
            "webm" => "audio/webm",
            "ogg" or "oga" => "audio/ogg",
            "flac" => "audio/flac",
            _ => "audio/mpeg",
        };
    }
}
