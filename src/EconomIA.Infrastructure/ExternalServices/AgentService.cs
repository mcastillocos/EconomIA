using System.Diagnostics;
using EconomIA.Application.Interfaces;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using EconomIA.Infrastructure.Persistence;
using EconomIA.Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EconomIA.Infrastructure.ExternalServices;

public class AgentService : IAgentService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILlmService _llm;
    private readonly ILogger<AgentService> _logger;
    private readonly Dictionary<string, IAgent> _agents;

    public AgentService(IServiceProvider serviceProvider, ILlmService llm, ILogger<AgentService> logger, IEnumerable<IAgent> agents)
    {
        _serviceProvider = serviceProvider;
        _llm = llm;
        _logger = logger;
        _agents = agents.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<AgentResult> RunAgentAsync(string agentName, string input, Dictionary<string, string>? context = null, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EconomIADbContext>();

        var run = AgentRun.Create(agentName, input);
        run.MarkRunning();
        dbContext.AgentRuns.Add(run);
        await dbContext.SaveChangesAsync(ct);

        var sw = Stopwatch.StartNew();
        OpenTelemetryConfig.AgentRuns.Add(1, new KeyValuePair<string, object?>("agent.name", agentName));

        try
        {
            if (!_agents.TryGetValue(agentName, out var agent))
                throw new InvalidOperationException($"Agent '{agentName}' not found. Available: {string.Join(", ", _agents.Keys)}");

            var result = await agent.ExecuteAsync(_llm, input, context ?? new(), ct);

            sw.Stop();
            OpenTelemetryConfig.AgentDuration.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("agent.name", agentName), new KeyValuePair<string, object?>("agent.status", "completed"));

            run.MarkCompleted(result.Output, result.Sources);
            await dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Agent {Agent} completed successfully ({Elapsed}ms)", agentName, sw.ElapsedMilliseconds);

            return new AgentResult
            {
                RunId = run.Id,
                AgentName = agentName,
                Status = "completed",
                Output = result.Output,
                Sources = result.Sources
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            OpenTelemetryConfig.AgentDuration.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("agent.name", agentName), new KeyValuePair<string, object?>("agent.status", "failed"));

            run.MarkFailed(ex.Message);
            await dbContext.SaveChangesAsync(ct);

            _logger.LogError(ex, "Agent {Agent} failed", agentName);

            return new AgentResult
            {
                RunId = run.Id,
                AgentName = agentName,
                Status = "failed",
                Output = "",
                Error = ex.Message
            };
        }
    }
}

public interface IAgent
{
    string Name { get; }
    string Description { get; }
    Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct);
}

public record AgentOutput
{
    public string Output { get; init; } = string.Empty;
    public string? Sources { get; init; }
}
