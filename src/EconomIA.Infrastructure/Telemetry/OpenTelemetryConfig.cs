using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EconomIA.Infrastructure.Telemetry;

public static class OpenTelemetryConfig
{
    public const string ServiceName = "EconomIA.API";
    public const string MeterName = "EconomIA.Funds";

    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter FundsMeter = new(MeterName, "1.0.0");

    // ── Custom Metrics ─────────────────────────────────────────────────────
    public static readonly Counter<long> FundsUpdated =
        FundsMeter.CreateCounter<long>("economia.funds.updated", "funds", "Number of fund price updates processed");

    public static readonly Counter<long> CacheHits =
        FundsMeter.CreateCounter<long>("economia.cache.hits", "hits", "Cache hit count");

    public static readonly Counter<long> CacheMisses =
        FundsMeter.CreateCounter<long>("economia.cache.misses", "misses", "Cache miss count");

    public static readonly Histogram<double> LlmLatency =
        FundsMeter.CreateHistogram<double>("economia.llm.latency", "ms", "LLM request latency in milliseconds");

    public static readonly Counter<long> LlmRequests =
        FundsMeter.CreateCounter<long>("economia.llm.requests", "requests", "Total LLM requests");

    public static readonly Counter<long> LlmFailures =
        FundsMeter.CreateCounter<long>("economia.llm.failures", "failures", "Failed LLM requests");

    public static readonly Counter<long> AgentRuns =
        FundsMeter.CreateCounter<long>("economia.agents.runs", "runs", "Total agent executions");

    public static readonly Histogram<double> AgentDuration =
        FundsMeter.CreateHistogram<double>("economia.agents.duration", "ms", "Agent execution duration in milliseconds");

    public static IServiceCollection AddOpenTelemetryObservability(
        this IServiceCollection services,
        string otlpEndpoint = "http://otel-collector:4317")
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: ServiceName, serviceVersion: "1.0.0"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(o =>
                {
                    o.RecordException = true;
                    o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddSource(ServiceName)
                .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(MeterName)
                .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint))
                .AddPrometheusExporter());

        services.AddLogging(logging =>
        {
            logging.AddOpenTelemetry(o =>
            {
                o.IncludeScopes = true;
                o.IncludeFormattedMessage = true;
                o.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName));
                o.AddOtlpExporter(e => e.Endpoint = new Uri(otlpEndpoint));
            });
        });

        return services;
    }
}
