using EconomIA.Application;
using EconomIA.Infrastructure;
using EconomIA.Infrastructure.Persistence;
using EconomIA.API.Hubs;
using EconomIA.API.Middleware;
using EconomIA.API.BackgroundServices;
using EconomIA.Application.Interfaces;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Layers DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// SignalR
builder.Services.AddSignalR()
    .AddMessagePackProtocol();

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EconomIA API", Version = "v1" });
});

// CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetValue<string>("Frontend:Url") ?? "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Notification service (SignalR adapter)
builder.Services.AddScoped<IFundNotificationService, SignalRFundNotificationService>();

// Background services
builder.Services.AddHostedService<MarketDataPollingService>();

// Health checks
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("SqlServer") ?? "", name: "sqlserver", tags: ["ready"])
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", name: "redis", tags: ["ready"]);

var app = builder.Build();

// Auto-create missing tables for Phase D-F if they don't exist
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EconomIADbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChecklistTemplates')
        BEGIN
            CREATE TABLE ChecklistTemplates (
                Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                Name NVARCHAR(200) NOT NULL,
                Description NVARCHAR(1000) NULL,
                Category NVARCHAR(50) NOT NULL,
                IsBuiltIn BIT NOT NULL DEFAULT 0,
                CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            );
            CREATE TABLE ChecklistTemplateItems (
                Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                TemplateId UNIQUEIDENTIFIER NOT NULL REFERENCES ChecklistTemplates(Id) ON DELETE CASCADE,
                Text NVARCHAR(500) NOT NULL,
                Section NVARCHAR(100) NOT NULL,
                [Order] INT NOT NULL DEFAULT 0,
                ItemType NVARCHAR(20) NOT NULL DEFAULT 'boolean',
                HelpText NVARCHAR(500) NULL
            );
            CREATE TABLE ChecklistInstances (
                Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                TemplateId UNIQUEIDENTIFIER NOT NULL REFERENCES ChecklistTemplates(Id),
                EntityType NVARCHAR(50) NOT NULL,
                EntityName NVARCHAR(200) NOT NULL,
                EntityId UNIQUEIDENTIFIER NULL,
                Status NVARCHAR(20) NOT NULL DEFAULT 'in_progress',
                Notes NVARCHAR(MAX) NULL,
                CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            );
            CREATE TABLE ChecklistAnswers (
                Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                InstanceId UNIQUEIDENTIFIER NOT NULL REFERENCES ChecklistInstances(Id) ON DELETE CASCADE,
                TemplateItemId UNIQUEIDENTIFIER NOT NULL REFERENCES ChecklistTemplateItems(Id),
                Value NVARCHAR(500) NOT NULL,
                Comment NVARCHAR(1000) NULL,
                AnsweredAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            );
        END
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EarningsCalls')
        BEGIN
            CREATE TABLE EarningsCalls (
                Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                CompanyName NVARCHAR(200) NOT NULL,
                Ticker NVARCHAR(20) NULL,
                CompanyId UNIQUEIDENTIFIER NULL,
                FiscalYear INT NOT NULL,
                FiscalQuarter INT NOT NULL,
                CallDate DATETIME2 NOT NULL,
                AudioFilePath NVARCHAR(500) NULL,
                TranscriptText NVARCHAR(MAX) NULL,
                Summary NVARCHAR(MAX) NULL,
                Guidance NVARCHAR(MAX) NULL,
                KeyMetrics NVARCHAR(MAX) NULL,
                Sentiment NVARCHAR(20) NULL,
                Status NVARCHAR(20) NOT NULL DEFAULT 'pending',
                ErrorMessage NVARCHAR(1000) NULL,
                DurationSeconds INT NULL,
                Language NVARCHAR(10) NULL,
                CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            );
        END
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InvestorProfiles')
        BEGIN
            CREATE TABLE InvestorProfiles (
                Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                UserId NVARCHAR(100) NOT NULL DEFAULT 'default',
                RiskTolerance NVARCHAR(20) NOT NULL DEFAULT 'moderate',
                InvestmentHorizon NVARCHAR(20) NOT NULL DEFAULT 'medium',
                MaxExpenseRatio DECIMAL(5,2) NOT NULL DEFAULT 1.5,
                MinReturn1Y DECIMAL(8,2) NOT NULL DEFAULT 0,
                PreferredSectors NVARCHAR(MAX) NOT NULL DEFAULT '[]',
                PreferredGeographies NVARCHAR(MAX) NOT NULL DEFAULT '[]',
                ExcludedSectors NVARCHAR(MAX) NOT NULL DEFAULT '[]',
                InvestmentStyle NVARCHAR(20) NOT NULL DEFAULT 'blend',
                AssetPreference NVARCHAR(20) NOT NULL DEFAULT 'both',
                EsgPreference BIT NOT NULL DEFAULT 0,
                Notes NVARCHAR(MAX) NULL,
                InteractionsCount INT NOT NULL DEFAULT 0,
                CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            );
            CREATE UNIQUE INDEX IX_InvestorProfiles_UserId ON InvestorProfiles(UserId);
        END
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ScreenerRecommendations')
        BEGIN
            CREATE TABLE ScreenerRecommendations (
                Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                ProfileId UNIQUEIDENTIFIER NOT NULL REFERENCES InvestorProfiles(Id),
                EntityType NVARCHAR(50) NOT NULL,
                EntityName NVARCHAR(200) NOT NULL,
                Ticker NVARCHAR(20) NULL,
                Isin NVARCHAR(20) NULL,
                Reason NVARCHAR(MAX) NOT NULL,
                Score DECIMAL(5,2) NOT NULL,
                Category NVARCHAR(20) NOT NULL,
                Metrics NVARCHAR(MAX) NULL,
                Status NVARCHAR(20) NOT NULL DEFAULT 'active',
                GeneratedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                ReviewedAt DATETIME2 NULL
            );
        END
        """);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "No se pudieron crear tablas automáticamente. Se crearán al ejecutar el script SQL manualmente.");
}

// Middleware
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseRouting();

app.MapControllers();
app.MapHub<FundPriceHub>("/hubs/fund-prices");
app.MapHub<RankingHub>("/hubs/ranking");

// Health endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // No checks — just confirms app is running
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health"); // All checks (backward compat)

// Prometheus metrics endpoint (only if OpenTelemetry is configured)
if (!string.IsNullOrWhiteSpace(app.Configuration["OpenTelemetry:OtlpEndpoint"]))
{
    app.UseOpenTelemetryPrometheusScrapingEndpoint();
}

app.Run();

public partial class Program { } // For integration tests
