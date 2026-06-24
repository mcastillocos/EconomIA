using EconomIA.Application;
using EconomIA.Infrastructure;
using EconomIA.API.Hubs;
using EconomIA.API.Middleware;
using EconomIA.API.BackgroundServices;
using EconomIA.Application.Interfaces;
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
    .AddSqlServer(builder.Configuration.GetConnectionString("SqlServer") ?? "")
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");

var app = builder.Build();

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
app.MapHealthChecks("/health");

app.Run();

public partial class Program { } // For integration tests
