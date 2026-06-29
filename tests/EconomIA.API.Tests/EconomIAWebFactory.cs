using EconomIA.Application.Interfaces;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using EconomIA.Domain.ValueObjects;
using EconomIA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace EconomIA.API.Tests;

public class EconomIAWebFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();
    private ILlmService? _llmService;

    public EconomIAWebFactory WithLlmService(ILlmService llm)
    {
        _llmService = llm;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remover todos los registros de DbContext y sus opciones
            var dbDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("DbContext") == true ||
                            d.ServiceType.FullName?.Contains("EntityFramework") == true ||
                            d.ImplementationType?.FullName?.Contains("SqlServer") == true)
                .ToList();
            foreach (var d in dbDescriptors) services.Remove(d);

            var optionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<EconomIADbContext>));
            if (optionsDescriptor != null) services.Remove(optionsDescriptor);

            services.AddDbContext<EconomIADbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Reemplazar Redis cache por no-op en tests
            var redisDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (redisDescriptor != null) services.Remove(redisDescriptor);

            // Reemplazar ICacheService por no-op (evita NullRef de Redis)
            var cacheDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ICacheService));
            if (cacheDescriptor != null) services.Remove(cacheDescriptor);
            services.AddSingleton<ICacheService, NullCacheService>();

            // Reemplazar EventBus por no-op
            var eventBusDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEventBus));
            if (eventBusDescriptor != null) services.Remove(eventBusDescriptor);
            services.AddSingleton<IEventBus, NullEventBus>();

            // Reemplazar health checks por uno vacío (no necesitan infra real)
            var hcDescriptors = services
                .Where(d => d.ServiceType == typeof(HealthCheckRegistration) ||
                            d.ServiceType.FullName?.Contains("HealthCheck") == true)
                .ToList();
            foreach (var hc in hcDescriptors) services.Remove(hc);

            // Re-registrar health checks sin infra
            var hcBuilder = services.AddHealthChecks();

            // Remover hosted services que necesitan infra real
            var hostedServices = services
                .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
                .ToList();
            foreach (var hs in hostedServices) services.Remove(hs);

            // Registrar ILlmService fake si se proporcionó
            if (_llmService is not null)
            {
                var llmDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ILlmService));
                if (llmDescriptor != null) services.Remove(llmDescriptor);
                services.AddSingleton(_llmService);
            }

            // Asegurar que la DB InMemory se crea al iniciar
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EconomIADbContext>();
            db.Database.EnsureCreated();
        });
    }

    public async Task SeedFundsAsync(params Fund[] funds)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EconomIADbContext>();
        await db.Database.EnsureCreatedAsync();
        db.Funds.AddRange(funds);
        await db.SaveChangesAsync();
    }

    public static Fund CreateTestFund(string name, RiskLevel risk = RiskLevel.Medium,
        string category = "Renta Variable", FundRating rating = FundRating.ThreeStars,
        int ranking = 1, decimal ter = 1.5m)
    {
        var fund = Fund.Create(
            new ISIN($"IE00B{Guid.NewGuid().ToString()[..6].ToUpper()}0"),
            name, category, "TestGestora", risk,
            new Money(100m, "EUR"), Percentage.FromDecimal(ter));
        fund.UpdateRanking(ranking, rating);
        return fund;
    }
}

internal class NullEventBus : IEventBus
{
    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IDomainEvent => Task.CompletedTask;
}

internal class NullCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) => Task.FromResult<T?>(default);
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) => Task.CompletedTask;
    public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> ExistsAsync(string key, CancellationToken ct = default) => Task.FromResult(false);
}
