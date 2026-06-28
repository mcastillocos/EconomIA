using System.Diagnostics.Metrics;
using EconomIA.Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EconomIA.Infrastructure.Tests.Telemetry;

public class OpenTelemetryConfigTests
{
    [Fact]
    public void ServiceName_ShouldBeEconomIAAPI()
    {
        Assert.Equal("EconomIA.API", OpenTelemetryConfig.ServiceName);
    }

    [Fact]
    public void MeterName_ShouldBeEconomIAFunds()
    {
        Assert.Equal("EconomIA.Funds", OpenTelemetryConfig.MeterName);
    }

    [Fact]
    public void ActivitySource_ShouldExist()
    {
        Assert.NotNull(OpenTelemetryConfig.ActivitySource);
        Assert.Equal("EconomIA.API", OpenTelemetryConfig.ActivitySource.Name);
    }

    [Fact]
    public void FundsMeter_ShouldHaveCorrectName()
    {
        Assert.NotNull(OpenTelemetryConfig.FundsMeter);
        Assert.Equal("EconomIA.Funds", OpenTelemetryConfig.FundsMeter.Name);
    }

    [Fact]
    public void CustomMetrics_ShouldBeCreated()
    {
        Assert.NotNull(OpenTelemetryConfig.FundsUpdated);
        Assert.NotNull(OpenTelemetryConfig.CacheHits);
        Assert.NotNull(OpenTelemetryConfig.CacheMisses);
        Assert.NotNull(OpenTelemetryConfig.LlmLatency);
        Assert.NotNull(OpenTelemetryConfig.LlmRequests);
        Assert.NotNull(OpenTelemetryConfig.LlmFailures);
        Assert.NotNull(OpenTelemetryConfig.AgentRuns);
        Assert.NotNull(OpenTelemetryConfig.AgentDuration);
    }

    [Fact]
    public void FundsUpdated_CanRecordWithoutError()
    {
        var ex = Record.Exception(() => OpenTelemetryConfig.FundsUpdated.Add(5));
        Assert.Null(ex);
    }

    [Fact]
    public void CacheHits_CanRecordWithTags()
    {
        var ex = Record.Exception(() =>
            OpenTelemetryConfig.CacheHits.Add(1, new KeyValuePair<string, object?>("cache.key", "test-key")));
        Assert.Null(ex);
    }

    [Fact]
    public void LlmLatency_CanRecordWithTags()
    {
        var ex = Record.Exception(() =>
            OpenTelemetryConfig.LlmLatency.Record(150.5, new KeyValuePair<string, object?>("llm.provider", "AzureOpenAI")));
        Assert.Null(ex);
    }

    [Fact]
    public void AgentDuration_CanRecordWithMultipleTags()
    {
        var ex = Record.Exception(() =>
            OpenTelemetryConfig.AgentDuration.Record(2500.0,
                new KeyValuePair<string, object?>("agent.name", "CompanyAnalysis"),
                new KeyValuePair<string, object?>("agent.status", "completed")));
        Assert.Null(ex);
    }

    [Fact]
    public void AddOpenTelemetryObservability_RegistersServices()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();

        var ex = Record.Exception(() => services.AddOpenTelemetryObservability("http://localhost:4317"));
        Assert.Null(ex);

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider);
    }
}
