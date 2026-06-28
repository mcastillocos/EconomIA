namespace EconomIA.Domain.Entities;

public class AgentRun : Entity<Guid>
{
    public string AgentName { get; private set; } = string.Empty;
    public string Status { get; private set; } = "pending"; // "pending" | "running" | "completed" | "failed"
    public string? Input { get; private set; }
    public string? Output { get; private set; }
    public string? Sources { get; private set; }
    public string? Error { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private AgentRun() { }

    public static AgentRun Create(string agentName, string? input = null)
    {
        if (string.IsNullOrWhiteSpace(agentName))
            throw new Exceptions.DomainException("Agent name cannot be empty.");

        return new AgentRun
        {
            Id = Guid.NewGuid(),
            AgentName = agentName,
            Status = "pending",
            Input = input,
            StartedAt = DateTime.UtcNow
        };
    }

    public void MarkRunning()
    {
        Status = "running";
    }

    public void MarkCompleted(string? output, string? sources)
    {
        Status = "completed";
        Output = output;
        Sources = sources;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = "failed";
        Error = error;
        CompletedAt = DateTime.UtcNow;
    }
}
