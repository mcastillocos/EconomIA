using EconomIA.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace EconomIA.Infrastructure.Workflows;

/// <summary>
/// Motor de ejecución de workflows multi-agente.
/// Permite encadenar agentes en pasos secuenciales o paralelos,
/// propagando contexto entre cada paso.
/// </summary>
public class WorkflowEngine
{
    private readonly IAgentService _agentService;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(IAgentService agentService, ILogger<WorkflowEngine> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta un workflow completo, paso a paso, propagando outputs como contexto.
    /// </summary>
    public async Task<WorkflowResult> ExecuteAsync(WorkflowDefinition workflow, string input, CancellationToken ct = default)
    {
        _logger.LogInformation("Iniciando workflow '{Name}' con {Steps} pasos", workflow.Name, workflow.Steps.Count);

        var result = new WorkflowResult
        {
            WorkflowName = workflow.Name,
            StartedAt = DateTime.UtcNow,
        };

        var accumulatedContext = new Dictionary<string, string>
        {
            ["workflow_name"] = workflow.Name,
            ["original_input"] = input,
        };

        foreach (var step in workflow.Steps)
        {
            ct.ThrowIfCancellationRequested();

            var stepResult = step.Parallel
                ? await ExecuteParallelStepAsync(step, input, accumulatedContext, ct)
                : await ExecuteSequentialStepAsync(step, input, accumulatedContext, ct);

            result.StepResults.Add(stepResult);

            if (stepResult.Status == "failed" && !step.ContinueOnFailure)
            {
                _logger.LogWarning("Workflow '{Name}' abortado en paso '{Step}': {Error}",
                    workflow.Name, step.Name, stepResult.Error);
                result.Status = "failed";
                result.CompletedAt = DateTime.UtcNow;
                return result;
            }

            // Propagar output al contexto para siguientes pasos
            if (stepResult.Status == "completed")
            {
                accumulatedContext[$"step_{step.Name}_output"] = stepResult.Output;
                input = BuildNextInput(step, stepResult, input);
            }
        }

        result.Status = "completed";
        result.CompletedAt = DateTime.UtcNow;
        result.FinalOutput = result.StepResults.LastOrDefault(s => s.Status == "completed")?.Output ?? "";

        _logger.LogInformation("Workflow '{Name}' completado en {Elapsed}ms",
            workflow.Name, (result.CompletedAt.Value - result.StartedAt).TotalMilliseconds);

        return result;
    }

    private async Task<WorkflowStepResult> ExecuteSequentialStepAsync(
        WorkflowStep step, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        _logger.LogInformation("  → Ejecutando paso '{Step}' (agente: {Agent})", step.Name, step.AgentName);

        var stepInput = step.InputTransform?.Invoke(input, context) ?? input;
        var stepContext = new Dictionary<string, string>(context);
        foreach (var (key, value) in step.ExtraContext)
            stepContext[key] = value;

        var agentResult = await _agentService.RunAgentAsync(step.AgentName, stepInput, stepContext, ct);

        return new WorkflowStepResult
        {
            StepName = step.Name,
            AgentName = step.AgentName,
            Status = agentResult.Status,
            Output = agentResult.Output,
            Sources = agentResult.Sources,
            Error = agentResult.Error,
        };
    }

    private async Task<WorkflowStepResult> ExecuteParallelStepAsync(
        WorkflowStep step, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        if (step.ParallelAgents.Count == 0)
            return await ExecuteSequentialStepAsync(step, input, context, ct);

        _logger.LogInformation("  → Ejecutando paso paralelo '{Step}' ({Count} agentes)",
            step.Name, step.ParallelAgents.Count);

        var tasks = step.ParallelAgents.Select(async agentName =>
        {
            var stepContext = new Dictionary<string, string>(context);
            foreach (var (key, value) in step.ExtraContext)
                stepContext[key] = value;

            return await _agentService.RunAgentAsync(agentName, input, stepContext, ct);
        });

        var results = await Task.WhenAll(tasks);
        var combinedOutput = string.Join("\n\n---\n\n", results.Where(r => r.Status == "completed").Select(r => r.Output));
        var anyFailed = results.Any(r => r.Status == "failed");

        return new WorkflowStepResult
        {
            StepName = step.Name,
            AgentName = string.Join("+", step.ParallelAgents),
            Status = anyFailed ? "partial" : "completed",
            Output = combinedOutput,
            Sources = string.Join("; ", results.Where(r => r.Sources != null).Select(r => r.Sources)),
        };
    }

    private static string BuildNextInput(WorkflowStep step, WorkflowStepResult result, string originalInput)
    {
        if (step.OutputAsNextInput)
            return result.Output;

        // Por defecto mantiene el input original enriquecido con contexto
        return originalInput;
    }
}

#region Models

public class WorkflowDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<WorkflowStep> Steps { get; init; } = [];
}

public class WorkflowStep
{
    public string Name { get; init; } = string.Empty;
    public string AgentName { get; init; } = string.Empty;
    public bool Parallel { get; init; }
    public List<string> ParallelAgents { get; init; } = [];
    public bool ContinueOnFailure { get; init; }
    public bool OutputAsNextInput { get; init; } = true;
    public Dictionary<string, string> ExtraContext { get; init; } = new();
    public Func<string, Dictionary<string, string>, string>? InputTransform { get; init; }
}

public class WorkflowResult
{
    public string WorkflowName { get; init; } = string.Empty;
    public string Status { get; set; } = "running";
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string FinalOutput { get; set; } = string.Empty;
    public List<WorkflowStepResult> StepResults { get; init; } = [];
}

public class WorkflowStepResult
{
    public string StepName { get; init; } = string.Empty;
    public string AgentName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Output { get; init; } = string.Empty;
    public string? Sources { get; init; }
    public string? Error { get; init; }
}

#endregion
