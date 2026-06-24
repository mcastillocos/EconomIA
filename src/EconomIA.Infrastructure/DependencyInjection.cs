using EconomIA.Domain.Ports;
using EconomIA.Domain.Services;
using EconomIA.Infrastructure.Cache;
using EconomIA.Infrastructure.ExternalServices;
using EconomIA.Infrastructure.Messaging;
using EconomIA.Infrastructure.Persistence;
using EconomIA.Infrastructure.Persistence.Repositories;
using EconomIA.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace EconomIA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // SQL Server
        services.AddDbContext<EconomIADbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("SqlServer"),
                b => b.MigrationsAssembly(typeof(EconomIADbContext).Assembly.FullName)));

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379"));

        // Kafka
        services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));

        // Repositories
        services.AddScoped<IFundRepository, FundRepository>();

        // Cache
        services.AddScoped<ICacheService, RedisCacheService>();

        // Event Bus
        services.AddSingleton<IEventBus, KafkaEventBus>();

        // External Services
        services.AddScoped<IMarketDataProvider, SimulatedMarketDataProvider>();

        // Domain Services
        services.AddScoped<FundRankingService>();

        // Kafka Consumer
        services.AddHostedService<KafkaConsumerService>();

        // OpenTelemetry
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
        services.AddOpenTelemetryObservability(otlpEndpoint);

        return services;
    }
}
