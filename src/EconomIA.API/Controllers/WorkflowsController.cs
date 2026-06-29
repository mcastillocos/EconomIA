using EconomIA.Infrastructure.Workflows;
using Microsoft.AspNetCore.Mvc;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowsController : ControllerBase
{
    private readonly IServiceProvider _sp;

    public WorkflowsController(IServiceProvider sp)
    {
        _sp = sp;
    }

    /// <summary>
    /// Lista todos los workflows disponibles.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkflowSummary>), StatusCodes.Status200OK)]
    public IActionResult ListWorkflows()
    {
        var workflows = PredefinedWorkflows.All.Select(w => new WorkflowSummary
        {
            Name = w.Name,
            Description = w.Description,
            Steps = w.Steps.Select(s => s.Name).ToList(),
        });
        return Ok(workflows);
    }

    /// <summary>
    /// Ejecuta un workflow por nombre.
    /// </summary>
    [HttpPost("{workflowName}")]
    [ProducesResponseType(typeof(WorkflowResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteWorkflow(
        string workflowName,
        [FromBody] WorkflowRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
            return BadRequest("El input no puede estar vacío.");

        var workflow = PredefinedWorkflows.GetByName(workflowName);
        if (workflow is null)
            return NotFound($"Workflow '{workflowName}' no encontrado. Disponibles: {string.Join(", ", PredefinedWorkflows.All.Select(w => w.Name))}");

        var engine = _sp.GetRequiredService<WorkflowEngine>();
        var result = await engine.ExecuteAsync(workflow, request.Input, ct);
        return Ok(result);
    }
}

public record WorkflowRequest
{
    public string Input { get; init; } = string.Empty;
}

public record WorkflowSummary
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Steps { get; init; } = [];
}
