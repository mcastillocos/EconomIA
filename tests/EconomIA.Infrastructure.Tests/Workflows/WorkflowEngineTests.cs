using EconomIA.Application.Interfaces;
using EconomIA.Infrastructure.Workflows;
using Microsoft.Extensions.Logging;
using Moq;

namespace EconomIA.Infrastructure.Tests.Workflows;

public class WorkflowEngineTests
{
    private readonly Mock<IAgentService> _agentServiceMock = new();
    private readonly WorkflowEngine _engine;

    public WorkflowEngineTests()
    {
        _engine = new WorkflowEngine(_agentServiceMock.Object, new Mock<ILogger<WorkflowEngine>>().Object);
    }

    [Fact]
    public async Task ExecuteAsync_SingleStepWorkflow_ShouldComplete()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "test_single",
            Steps = [new() { Name = "paso1", AgentName = "company_analysis" }],
        };

        _agentServiceMock
            .Setup(x => x.RunAgentAsync("company_analysis", It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult { Status = "completed", Output = "Análisis de Inditex", AgentName = "company_analysis" });

        var result = await _engine.ExecuteAsync(workflow, "Inditex");

        Assert.Equal("completed", result.Status);
        Assert.Single(result.StepResults);
        Assert.Equal("Análisis de Inditex", result.FinalOutput);
    }

    [Fact]
    public async Task ExecuteAsync_MultiStepWorkflow_ShouldPropagateContext()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "test_multi",
            Steps =
            [
                new() { Name = "paso1", AgentName = "daily_news", OutputAsNextInput = true },
                new() { Name = "paso2", AgentName = "portfolio_briefing" },
            ],
        };

        _agentServiceMock
            .Setup(x => x.RunAgentAsync("daily_news", It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult { Status = "completed", Output = "Noticias del día", AgentName = "daily_news" });

        _agentServiceMock
            .Setup(x => x.RunAgentAsync("portfolio_briefing", It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult { Status = "completed", Output = "Briefing consolidado", AgentName = "portfolio_briefing" });

        var result = await _engine.ExecuteAsync(workflow, "Mi cartera");

        Assert.Equal("completed", result.Status);
        Assert.Equal(2, result.StepResults.Count);
        Assert.Equal("Briefing consolidado", result.FinalOutput);
    }

    [Fact]
    public async Task ExecuteAsync_StepFails_ShouldAbortWorkflow()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "test_fail",
            Steps =
            [
                new() { Name = "paso1", AgentName = "company_analysis" },
                new() { Name = "paso2", AgentName = "risk" },
            ],
        };

        _agentServiceMock
            .Setup(x => x.RunAgentAsync("company_analysis", It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult { Status = "failed", Error = "LLM timeout", AgentName = "company_analysis" });

        var result = await _engine.ExecuteAsync(workflow, "Tesla");

        Assert.Equal("failed", result.Status);
        Assert.Single(result.StepResults);
        Assert.Equal("failed", result.StepResults[0].Status);
    }

    [Fact]
    public async Task ExecuteAsync_StepFailsWithContinueOnFailure_ShouldContinue()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "test_continue",
            Steps =
            [
                new() { Name = "paso1", AgentName = "data_validation", ContinueOnFailure = true },
                new() { Name = "paso2", AgentName = "risk" },
            ],
        };

        _agentServiceMock
            .Setup(x => x.RunAgentAsync("data_validation", It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult { Status = "failed", Error = "No data", AgentName = "data_validation" });

        _agentServiceMock
            .Setup(x => x.RunAgentAsync("risk", It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult { Status = "completed", Output = "Riesgo medio", AgentName = "risk" });

        var result = await _engine.ExecuteAsync(workflow, "Fondo X");

        Assert.Equal("completed", result.Status);
        Assert.Equal(2, result.StepResults.Count);
        Assert.Equal("Riesgo medio", result.FinalOutput);
    }

    [Fact]
    public async Task ExecuteAsync_ParallelStep_ShouldRunAgentsInParallel()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "test_parallel",
            Steps =
            [
                new()
                {
                    Name = "analisis_paralelo",
                    Parallel = true,
                    AgentName = "financial_data_extractor",
                    ParallelAgents = ["financial_data_extractor", "risk"],
                },
            ],
        };

        _agentServiceMock
            .Setup(x => x.RunAgentAsync("financial_data_extractor", It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult { Status = "completed", Output = "Datos extraídos", AgentName = "financial_data_extractor" });

        _agentServiceMock
            .Setup(x => x.RunAgentAsync("risk", It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult { Status = "completed", Output = "Evaluación de riesgo", AgentName = "risk" });

        var result = await _engine.ExecuteAsync(workflow, "Inditex");

        Assert.Equal("completed", result.Status);
        Assert.Contains("Datos extraídos", result.StepResults[0].Output);
        Assert.Contains("Evaluación de riesgo", result.StepResults[0].Output);
    }

    [Fact]
    public async Task ExecuteAsync_WithInputTransform_ShouldTransformInput()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "test_transform",
            Steps =
            [
                new() { Name = "paso1", AgentName = "company_analysis", OutputAsNextInput = true },
                new()
                {
                    Name = "paso2",
                    AgentName = "comparison",
                    InputTransform = (input, ctx) => $"Compara basado en: {ctx.GetValueOrDefault("step_paso1_output", "")}",
                },
            ],
        };

        _agentServiceMock
            .Setup(x => x.RunAgentAsync("company_analysis", It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult { Status = "completed", Output = "Análisis Apple", AgentName = "company_analysis" });

        _agentServiceMock
            .Setup(x => x.RunAgentAsync("comparison", "Compara basado en: Análisis Apple", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult { Status = "completed", Output = "Comparación hecha", AgentName = "comparison" });

        var result = await _engine.ExecuteAsync(workflow, "Apple");

        Assert.Equal("completed", result.Status);
        Assert.Equal("Comparación hecha", result.FinalOutput);
    }
}

public class PredefinedWorkflowsTests
{
    [Fact]
    public void All_ShouldReturnFourWorkflows()
    {
        var all = PredefinedWorkflows.All;
        Assert.Equal(4, all.Count);
    }

    [Theory]
    [InlineData("morning_briefing")]
    [InlineData("company_research")]
    [InlineData("fund_due_diligence")]
    [InlineData("annual_report_analysis")]
    public void GetByName_ShouldFindWorkflow(string name)
    {
        var wf = PredefinedWorkflows.GetByName(name);
        Assert.NotNull(wf);
        Assert.Equal(name, wf.Name);
        Assert.NotEmpty(wf.Steps);
    }

    [Fact]
    public void GetByName_Unknown_ShouldReturnNull()
    {
        Assert.Null(PredefinedWorkflows.GetByName("nonexistent"));
    }

    [Fact]
    public void MorningBriefing_ShouldHaveThreeSteps()
    {
        var wf = PredefinedWorkflows.MorningBriefing;
        Assert.Equal(3, wf.Steps.Count);
        Assert.Equal("noticias", wf.Steps[0].Name);
        Assert.Equal("riesgos", wf.Steps[1].Name);
        Assert.Equal("briefing_final", wf.Steps[2].Name);
    }

    [Fact]
    public void CompanyResearch_ShouldHaveParallelStep()
    {
        var wf = PredefinedWorkflows.CompanyResearch;
        Assert.True(wf.Steps[1].Parallel);
        Assert.Contains("financial_data_extractor", wf.Steps[1].ParallelAgents);
        Assert.Contains("risk", wf.Steps[1].ParallelAgents);
    }

    [Fact]
    public void FundDueDiligence_ShouldHaveFourSteps()
    {
        var wf = PredefinedWorkflows.FundDueDiligence;
        Assert.Equal(4, wf.Steps.Count);
        Assert.True(wf.Steps[1].ContinueOnFailure);
    }
}
