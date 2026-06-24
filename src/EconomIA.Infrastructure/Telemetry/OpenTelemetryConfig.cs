using System.Diagnostics;
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
    public static readonly ActivitySource ActivitySource = new(ServiceName);

    public static IServiceCollection AddOpenTelemetryObservability(
        this IServiceCollection services,
        string otlpEndpoint = "http://otel-collector:4317")
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: ServiceName, serviceVersion: "1.0.0"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddSource(ServiceName)
                .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter("EconomIA.Funds")
                .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));

        services.AddLogging(logging =>
        {
            logging.AddOpenTelemetry(o =>
            {
                o.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName));
                o.AddOtlpExporter(e => e.Endpoint = new Uri(otlpEndpoint));
            });
        });

        return services;
    }
}
