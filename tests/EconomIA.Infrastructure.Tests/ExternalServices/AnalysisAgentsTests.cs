using EconomIA.Application.Interfaces;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using EconomIA.Infrastructure.ExternalServices;
using EconomIA.Infrastructure.ExternalServices.Agents;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace EconomIA.Infrastructure.Tests.ExternalServices;

public class AnalysisAgentsTests
{
    private IServiceProvider BuildServiceProvider(
        IReadOnlyList<Company>? companies = null,
        IReadOnlyList<Watchlist>? watchlists = null)
    {
        var companyRepo = new Mock<ICompanyRepository>();
        companyRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(companies ?? new List<Company>());

        var watchlistRepo = new Mock<IWatchlistRepository>();
        watchlistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(watchlists ?? new List<Watchlist>());

        var fundRepo = new Mock<IFundRepository>();
        fundRepo.Setup(r => r.GetTopFundsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Entities.Fund>());

        var metricRepo = new Mock<IFinancialMetricRepository>();
        metricRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinancialMetric>());
        metricRepo.Setup(r => r.GetByEntityAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinancialMetric>());

        var services = new ServiceCollection();
        services.AddScoped(_ => companyRepo.Object);
        services.AddScoped(_ => watchlistRepo.Object);
        services.AddScoped(_ => fundRepo.Object);
        services.AddScoped(_ => metricRepo.Object);
        return services.BuildServiceProvider();
    }

    private static Mock<ILlmService> MockLlm(string response = "Briefing generado correctamente.")
    {
        var llm = new Mock<ILlmService>();
        llm.Setup(l => l.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<LlmOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Content = response,
                Provider = "Test",
                Model = "test-model",
                PromptTokens = 100,
                CompletionTokens = 200,
                TotalTokens = 300
            });
        return llm;
    }

    [Fact]
    public async Task DailyNewsAgent_GeneratesBriefing()
    {
        var sp = BuildServiceProvider(
            companies: new List<Company> { Company.Create("TestCorp", "TST", "ES0000000001", country: "España", sector: "Tech") });
        var agent = new DailyNewsAgent(sp);
        var llm = MockLlm("## Briefing\nHoy no hay novedades significativas.");

        var result = await agent.ExecuteAsync(llm.Object, "sector tech", new(), CancellationToken.None);

        Assert.Equal("## Briefing\nHoy no hay novedades significativas.", result.Output);
        Assert.Contains("empresas", result.Sources);
    }

    [Fact]
    public async Task ScreenerAgent_FiltersWithCriteria()
    {
        var sp = BuildServiceProvider(
            companies: new List<Company> { Company.Create("Inditex", "ITX", "ES0148396007", country: "España", sector: "Consumer") });
        var agent = new ScreenerAgent(sp);
        var llm = MockLlm("## Resultados\n1. Inditex cumple parcialmente.");

        var result = await agent.ExecuteAsync(llm.Object, "ROE > 15%", new(), CancellationToken.None);

        Assert.Contains("Inditex cumple", result.Output);
        Assert.Contains("empresas", result.Sources);
    }

    [Fact]
    public async Task PortfolioBriefingAgent_GeneratesSummary()
    {
        var sp = BuildServiceProvider(
            companies: new List<Company>
            {
                Company.Create("Apple", "AAPL", "US0378331005", country: "EEUU", sector: "Tech"),
                Company.Create("LVMH", "MC", "FR0000121014", country: "Francia", sector: "Luxury")
            });
        var agent = new PortfolioBriefingAgent(sp);
        var llm = MockLlm("## Resumen Cartera\n2 posiciones, concentración en tech.");

        var result = await agent.ExecuteAsync(llm.Object, "", new(), CancellationToken.None);

        Assert.Contains("Resumen Cartera", result.Output);
        Assert.Contains("empresas", result.Sources);
    }

    [Fact]
    public async Task DailyNewsAgent_EmptyPortfolio_StillWorks()
    {
        var sp = BuildServiceProvider();
        var agent = new DailyNewsAgent(sp);
        var llm = MockLlm("Sin posiciones registradas.");

        var result = await agent.ExecuteAsync(llm.Object, "", new(), CancellationToken.None);

        Assert.Equal("Sin posiciones registradas.", result.Output);
    }

    [Fact]
    public async Task ScreenerAgent_NoData_ReportsEmpty()
    {
        var sp = BuildServiceProvider();
        var agent = new ScreenerAgent(sp);
        var llm = MockLlm("No hay datos disponibles para filtrar.");

        var result = await agent.ExecuteAsync(llm.Object, "PER < 10", new(), CancellationToken.None);

        Assert.Contains("No hay datos", result.Output);
    }
}
