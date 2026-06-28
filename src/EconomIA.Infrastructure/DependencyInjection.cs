using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using EconomIA.Domain.Services;
using EconomIA.Infrastructure.Cache;
using EconomIA.Infrastructure.Connectors;
using EconomIA.Infrastructure.ExternalServices;
using EconomIA.Infrastructure.ExternalServices.Agents;
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

        // Kafka (optional — skip if BootstrapServers is empty)
        var kafkaServers = configuration["Kafka:BootstrapServers"];
        if (!string.IsNullOrWhiteSpace(kafkaServers))
        {
            services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));
            services.AddSingleton<IEventBus, KafkaEventBus>();
            services.AddHostedService<KafkaConsumerService>();
        }
        else
        {
            services.AddSingleton<IEventBus, NoOpEventBus>();
        }

        // Repositories
        services.AddScoped<IFundRepository, FundRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IWatchlistRepository, WatchlistRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IFinancialMetricRepository, FinancialMetricRepository>();

        // Connectors
        services.AddSingleton<IDataConnector, CsvConnector>();
        services.AddSingleton<IDataConnector, ExcelConnector>();
        services.AddSingleton<IDataConnector, PdfConnector>();
        services.AddSingleton<IDataConnector, TikrConnector>();
        services.AddSingleton<IDataConnector, InvestingConnector>();
        services.AddSingleton<IDataConnector, EmailConnector>();
        services.AddSingleton<IDataConnector, NewsConnector>();
        services.AddSingleton<IDataConnector, TranscriptConnector>();
        services.AddSingleton<IDataConnector, AudioConnector>();
        services.AddSingleton<IDataConnector, ApiConnector>();
        services.AddSingleton<IDataConnector, ManualConnector>();

        // Cache
        services.AddScoped<ICacheService, RedisCacheService>();

        // External Services
        services.AddScoped<IMarketDataProvider, SimulatedMarketDataProvider>();

        // LLM Service
        services.AddHttpClient<ILlmService, LlmService>();

        // Agents
        services.AddScoped<IAgent, CompanyAnalysisAgent>();
        services.AddScoped<IAgent, FundAnalysisAgent>();
        services.AddScoped<IAgent, DailyNewsAgent>();
        services.AddScoped<IAgent, ScreenerAgent>();
        services.AddScoped<IAgent, PortfolioBriefingAgent>();
        services.AddScoped<IAgent, EarningsCallAgent>();
        services.AddScoped<IAgent, AnnualReportReaderAgent>();
        services.AddScoped<IAgent, DataValidationAgent>();
        services.AddScoped<IAgent, ComparisonAgent>();
        services.AddScoped<IAgentService, AgentService>();

        // Domain Services
        services.AddScoped<FundRankingService>();

        // OpenTelemetry (optional)
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            services.AddOpenTelemetryObservability(otlpEndpoint);
        }

        return services;
    }
}
