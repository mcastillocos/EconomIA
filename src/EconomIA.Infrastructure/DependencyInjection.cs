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
using EconomIA.Infrastructure.Scheduling;
using EconomIA.Infrastructure.Telemetry;
using EconomIA.Infrastructure.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
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
        services.AddSingleton<IDataConnector, EmailConnector>();
        services.AddScoped<IDataConnector, TranscriptConnectorReal>();
        services.AddScoped<IDataConnector, AudioConnectorReal>();
        services.AddSingleton<IDataConnector, ApiConnector>();
        services.AddSingleton<IDataConnector, ManualConnector>();
        services.AddScoped<ConnectorOrchestrator>();

        // Audio Transcription (OpenAI Whisper)
        services.AddHttpClient<IAudioTranscriptionService, WhisperTranscriptionService>();

        // Investing.com Connector (real, con HttpClient + resilience)
        services.AddHttpClient<InvestingConnector>()
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
            });
        services.AddSingleton<IDataConnector>(sp => sp.GetRequiredService<InvestingConnector>());

        // Financial Modeling Prep Connector (API real)
        services.AddHttpClient<FmpConnector>()
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
            });
        services.AddSingleton<IDataConnector>(sp => sp.GetRequiredService<FmpConnector>());

        // News Connector (real, con HttpClient + resilience)
        // Note: AudioConnectorReal and TranscriptConnectorReal registered above as Scoped (need ILlmService)
        services.AddHttpClient<RssNewsConnector>()
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
            });
        services.AddSingleton<IDataConnector>(sp => sp.GetRequiredService<RssNewsConnector>());

        // Briefing Scheduler
        services.AddHostedService<BriefingSchedulerService>();

        // Cache
        services.AddScoped<ICacheService, RedisCacheService>();

        // External Services - Market Data with resilience
        services.AddHttpClient<IMarketDataProvider, YahooFinanceProvider>()
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromMilliseconds(500);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.MinimumThroughput = 5;
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
            });
        // Fallback: simulated provider when Yahoo is unavailable
        services.AddScoped<SimulatedMarketDataProvider>();

        // LLM Service with resilience
        services.AddHttpClient<ILlmService, LlmService>()
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
            });

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
        services.AddScoped<IAgent, FinancialDataExtractorAgent>();
        services.AddScoped<IAgent, RiskAgent>();
        services.AddScoped<IAgentService, AgentService>();

        // Export Service (PDF/Excel)
        services.AddScoped<IExportService, ExportService>();

        // Workflow Engine
        services.AddScoped<WorkflowEngine>();

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
