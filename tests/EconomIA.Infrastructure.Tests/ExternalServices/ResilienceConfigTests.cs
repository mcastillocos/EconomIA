using EconomIA.Domain.Ports;
using EconomIA.Application.Interfaces;
using EconomIA.Infrastructure.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EconomIA.Infrastructure.Tests.ExternalServices;

public class ResilienceConfigTests
{
    private static IServiceProvider BuildServiceProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServer"] = "Server=.;Database=Test;Trusted_Connection=True;TrustServerCertificate=True",
                ["ConnectionStrings:Redis"] = "localhost:6379",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void MarketDataProvider_IsRegistered_AsYahooFinance()
    {
        var sp = BuildServiceProvider();
        using var scope = sp.CreateScope();

        // HttpClient factory should resolve the provider
        var provider = scope.ServiceProvider.GetService<IMarketDataProvider>();

        Assert.NotNull(provider);
        Assert.IsType<YahooFinanceProvider>(provider);
    }

    [Fact]
    public void LlmService_IsRegistered_InServiceCollection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServer"] = "Server=.;Database=Test;Trusted_Connection=True;TrustServerCertificate=True",
                ["ConnectionStrings:Redis"] = "localhost:6379",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(config);

        // Verify service descriptor exists (ILlmService -> LlmService)
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ILlmService));
        Assert.NotNull(descriptor);
    }

    [Fact]
    public void SimulatedProvider_IsRegistered_AsFallback()
    {
        var sp = BuildServiceProvider();
        using var scope = sp.CreateScope();

        var simulated = scope.ServiceProvider.GetService<SimulatedMarketDataProvider>();

        Assert.NotNull(simulated);
    }
}
