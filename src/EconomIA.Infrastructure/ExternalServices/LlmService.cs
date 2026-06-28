using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EconomIA.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EconomIA.Infrastructure.ExternalServices;

public class LlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<LlmService> _logger;
    private readonly List<LlmProvider> _providers;
    private int _rotationIndex;

    public LlmService(HttpClient httpClient, IConfiguration config, ILogger<LlmService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _providers = BuildProviders();
    }

    public async Task<LlmResponse> ChatAsync(string systemPrompt, string userPrompt, LlmOptions? options = null, CancellationToken ct = default)
    {
        options ??= new LlmOptions();
        var available = _providers.Where(p => p.IsAvailable).ToList();

        if (available.Count == 0)
            throw new InvalidOperationException("No LLM providers configured. Set Azure OpenAI or Claude API keys.");

        // Round-robin with failover
        var startIdx = Interlocked.Increment(ref _rotationIndex) % available.Count;

        for (int attempt = 0; attempt < available.Count; attempt++)
        {
            var provider = available[(startIdx + attempt) % available.Count];
            try
            {
                var result = await provider.CallAsync(_httpClient, systemPrompt, userPrompt, options, ct);
                _logger.LogInformation("LLM call OK: {Provider} ({Tokens} tokens)", provider.Name, result.TotalTokens);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM provider {Provider} failed, trying next...", provider.Name);
                if (attempt == available.Count - 1)
                    throw new InvalidOperationException($"All LLM providers failed. Last error: {ex.Message}", ex);
            }
        }

        throw new InvalidOperationException("No LLM providers available.");
    }

    private List<LlmProvider> BuildProviders()
    {
        var providers = new List<LlmProvider>();
        var section = _config.GetSection("LLM");

        // Azure OpenAI
        var azureKey = section["AzureOpenAI:ApiKey"] ?? _config["AZURE_OPENAI_API_KEY0"];
        var azureEndpoint = section["AzureOpenAI:Endpoint"] ?? _config["AZURE_OPENAI_ENDPOINT0"];
        var azureDeployment = section["AzureOpenAI:Deployment"] ?? _config["AZURE_OPENAI_DEPLOYMENT0"];
        var azureVersion = section["AzureOpenAI:ApiVersion"] ?? _config["AZURE_OPENAI_API_VERSION0"] ?? "2024-08-01-preview";

        if (!string.IsNullOrWhiteSpace(azureKey) && !string.IsNullOrWhiteSpace(azureEndpoint))
        {
            providers.Add(new AzureOpenAIProvider(azureKey, azureEndpoint.TrimEnd('/'), azureDeployment ?? "gpt-5.5", azureVersion));
        }

        // Claude
        var claudeKey = section["Claude:ApiKey"] ?? _config["CLAUDE_API_KEY"];
        var claudeEndpoint = section["Claude:Endpoint"] ?? _config["CLAUDE_ENDPOINT"] ?? "https://api.anthropic.com/v1/messages";
        var claudeModel = section["Claude:Model"] ?? _config["CLAUDE_MODEL1"] ?? "claude-opus-4-7";

        if (!string.IsNullOrWhiteSpace(claudeKey))
        {
            providers.Add(new ClaudeProvider(claudeKey, claudeEndpoint, claudeModel));
        }

        return providers;
    }

    // ── Provider Implementations ──

    private abstract class LlmProvider
    {
        public abstract string Name { get; }
        public abstract bool IsAvailable { get; }
        public abstract Task<LlmResponse> CallAsync(HttpClient http, string system, string user, LlmOptions opts, CancellationToken ct);
    }

    private class AzureOpenAIProvider : LlmProvider
    {
        private readonly string _apiKey, _endpoint, _deployment, _apiVersion;

        public AzureOpenAIProvider(string apiKey, string endpoint, string deployment, string apiVersion)
        {
            _apiKey = apiKey;
            _endpoint = endpoint;
            _deployment = deployment;
            _apiVersion = apiVersion;
        }

        public override string Name => $"AzureOpenAI/{_deployment}";
        public override bool IsAvailable => true;

        public override async Task<LlmResponse> CallAsync(HttpClient http, string system, string user, LlmOptions opts, CancellationToken ct)
        {
            var url = $"{_endpoint}/openai/deployments/{_deployment}/chat/completions?api-version={_apiVersion}";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("api-key", _apiKey);
            request.Content = JsonContent.Create(new
            {
                messages = new[]
                {
                    new { role = "system", content = system },
                    new { role = "user", content = user }
                },
                max_completion_tokens = opts.MaxTokens,
                temperature = opts.Temperature
            });

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(90));

            var resp = await http.SendAsync(request, cts.Token);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cts.Token);
            var content = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
            var usage = json.GetProperty("usage");

            return new LlmResponse
            {
                Content = content,
                Provider = "AzureOpenAI",
                Model = _deployment,
                PromptTokens = usage.GetProperty("prompt_tokens").GetInt32(),
                CompletionTokens = usage.GetProperty("completion_tokens").GetInt32(),
                TotalTokens = usage.GetProperty("total_tokens").GetInt32()
            };
        }
    }

    private class ClaudeProvider : LlmProvider
    {
        private readonly string _apiKey, _endpoint, _model;

        public ClaudeProvider(string apiKey, string endpoint, string model)
        {
            _apiKey = apiKey;
            _endpoint = endpoint;
            _model = model;
        }

        public override string Name => $"Claude/{_model}";
        public override bool IsAvailable => true;

        public override async Task<LlmResponse> CallAsync(HttpClient http, string system, string user, LlmOptions opts, CancellationToken ct)
        {
            var isAzure = _endpoint.Contains(".azure.com", StringComparison.OrdinalIgnoreCase);

            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            if (isAzure)
            {
                request.Headers.Add("api-key", _apiKey);
            }
            else
            {
                request.Headers.Add("x-api-key", _apiKey);
            }
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = JsonContent.Create(new
            {
                model = _model,
                max_tokens = opts.MaxTokens,
                temperature = opts.Temperature,
                system,
                messages = new[] { new { role = "user", content = user } }
            });

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(90));

            var resp = await http.SendAsync(request, cts.Token);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cts.Token);
            var content = json.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
            var usage = json.GetProperty("usage");

            return new LlmResponse
            {
                Content = content,
                Provider = "Claude",
                Model = _model,
                PromptTokens = usage.GetProperty("input_tokens").GetInt32(),
                CompletionTokens = usage.GetProperty("output_tokens").GetInt32(),
                TotalTokens = usage.GetProperty("input_tokens").GetInt32() + usage.GetProperty("output_tokens").GetInt32()
            };
        }
    }
}
