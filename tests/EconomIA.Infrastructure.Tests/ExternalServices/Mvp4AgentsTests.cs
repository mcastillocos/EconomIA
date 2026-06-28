using EconomIA.Application.Interfaces;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using EconomIA.Infrastructure.ExternalServices;
using EconomIA.Infrastructure.ExternalServices.Agents;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace EconomIA.Infrastructure.Tests.ExternalServices;

public class Mvp4AgentsTests
{
    private IServiceProvider BuildServiceProvider(
        IReadOnlyList<UploadedDocument>? docs = null,
        IReadOnlyList<Company>? companies = null,
        IReadOnlyList<FinancialMetric>? metrics = null)
    {
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs ?? new List<UploadedDocument>());

        var companyRepo = new Mock<ICompanyRepository>();
        companyRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(companies ?? new List<Company>());

        var fundRepo = new Mock<IFundRepository>();
        fundRepo.Setup(r => r.GetTopFundsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fund>());

        var metricRepo = new Mock<IFinancialMetricRepository>();
        metricRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics ?? new List<FinancialMetric>());

        var watchlistRepo = new Mock<IWatchlistRepository>();
        watchlistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Watchlist>());

        var services = new ServiceCollection();
        services.AddScoped(_ => docRepo.Object);
        services.AddScoped(_ => companyRepo.Object);
        services.AddScoped(_ => fundRepo.Object);
        services.AddScoped(_ => metricRepo.Object);
        services.AddScoped(_ => watchlistRepo.Object);
        return services.BuildServiceProvider();
    }

    private static Mock<ILlmService> MockLlm(string response)
    {
        var llm = new Mock<ILlmService>();
        llm.Setup(l => l.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<LlmOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Content = response,
                Provider = "Test",
                Model = "test",
                PromptTokens = 50,
                CompletionTokens = 100,
                TotalTokens = 150
            });
        return llm;
    }

    [Fact]
    public async Task EarningsCallAgent_NoTranscripts_ReportsEmpty()
    {
        var sp = BuildServiceProvider();
        var agent = new EarningsCallAgent(sp);
        var llm = MockLlm("No hay transcripciones disponibles.");

        var result = await agent.ExecuteAsync(llm.Object, "Apple", new(), CancellationToken.None);

        Assert.Contains("No hay transcripciones", result.Output);
    }

    [Fact]
    public async Task EarningsCallAgent_WithTranscript_ExtractsData()
    {
        var doc = UploadedDocument.Create("company", Guid.NewGuid(), "apple_earnings_q4.txt", "transcript", "/tmp/apple.txt", 5000);
        var sp = BuildServiceProvider(docs: new List<UploadedDocument> { doc });
        var agent = new EarningsCallAgent(sp);
        var llm = MockLlm("## Guidance\nApple espera crecimiento del 8%.");

        var result = await agent.ExecuteAsync(llm.Object, "apple", new(), CancellationToken.None);

        Assert.Contains("Guidance", result.Output);
        Assert.Contains("apple_earnings_q4.txt", result.Sources);
    }

    [Fact]
    public async Task AnnualReportReaderAgent_NoReports_ReportsEmpty()
    {
        var sp = BuildServiceProvider();
        var agent = new AnnualReportReaderAgent(sp);
        var llm = MockLlm("No se encontraron informes.");

        var result = await agent.ExecuteAsync(llm.Object, "Inditex", new(), CancellationToken.None);

        Assert.Contains("No se encontraron", result.Output);
    }

    [Fact]
    public async Task DataValidationAgent_WithMetrics_ValidatesData()
    {
        var company = Company.Create("TestCo", "TST");
        var metric = FinancialMetric.Create("company", company.Id, "Revenue", 1500000m, year: 2025, source: "csv_upload");
        var sp = BuildServiceProvider(companies: new List<Company> { company }, metrics: new List<FinancialMetric> { metric });
        var agent = new DataValidationAgent(sp);
        var llm = MockLlm("## Resumen\n1 dato OK, 0 problemas.");

        var result = await agent.ExecuteAsync(llm.Object, "TestCo", new(), CancellationToken.None);

        Assert.Contains("1 dato OK", result.Output);
    }

    [Fact]
    public async Task ComparisonAgent_ComparesTwoEntities()
    {
        var c1 = Company.Create("Apple", "AAPL");
        var c2 = Company.Create("Microsoft", "MSFT");
        var sp = BuildServiceProvider(companies: new List<Company> { c1, c2 });
        var agent = new ComparisonAgent(sp);
        var llm = MockLlm("## Comparativa\nApple vs Microsoft: ambas tech.");

        var result = await agent.ExecuteAsync(llm.Object, "Apple vs Microsoft", new(), CancellationToken.None);

        Assert.Contains("Comparativa", result.Output);
        Assert.Contains("Apple", result.Sources);
        Assert.Contains("Microsoft", result.Sources);
    }

    [Fact]
    public async Task DataValidationAgent_EmptyMetrics_ReportsNoData()
    {
        var sp = BuildServiceProvider();
        var agent = new DataValidationAgent(sp);
        var llm = MockLlm("No hay métricas cargadas.");

        var result = await agent.ExecuteAsync(llm.Object, "", new(), CancellationToken.None);

        Assert.Contains("No hay métricas", result.Output);
    }
}
