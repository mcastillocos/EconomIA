namespace EconomIA.Application.Interfaces;

public interface ILlmService
{
    Task<LlmResponse> ChatAsync(string systemPrompt, string userPrompt, LlmOptions? options = null, CancellationToken ct = default);
}

public record LlmOptions
{
    public string? Model { get; init; }
    public double Temperature { get; init; } = 0.3;
    public int MaxTokens { get; init; } = 4000;
}

public record LlmResponse
{
    public string Content { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens { get; init; }
}
