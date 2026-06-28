namespace EconomIA.Application.Interfaces;

public interface IAgentService
{
    Task<AgentResult> RunAgentAsync(string agentName, string input, Dictionary<string, string>? context = null, CancellationToken ct = default);
}

public record AgentResult
{
    public Guid RunId { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty; // "completed" | "failed"
    public string Output { get; init; } = string.Empty;
    public string? Sources { get; init; }
    public string? Error { get; init; }
}
