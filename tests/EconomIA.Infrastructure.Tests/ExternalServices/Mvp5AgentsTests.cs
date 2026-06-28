using EconomIA.Application.Interfaces;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using EconomIA.Domain.ValueObjects;
using EconomIA.Infrastructure.ExternalServices;
using EconomIA.Infrastructure.ExternalServices.Agents;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace EconomIA.Infrastructure.Tests.ExternalServices;

public class Mvp5AgentsTests
{
    private IServiceProvider BuildServiceProvider(
        IReadOnlyList<UploadedDocument>? docs = null,
        IReadOnlyList<Company>? companies = null,
        IReadOnlyList<Fund>? funds = null,
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
            .ReturnsAsync(funds ?? new List<Fund>());

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

    // ── FinancialDataExtractorAgent Tests ──────────────────────────────────

    [Fact]
    public async Task FinancialDataExtractorAgent_NoDocs_ReportsEmpty()
    {
        var sp = BuildServiceProvider();
        var agent = new FinancialDataExtractorAgent(sp);
        var llm = MockLlm("No se encontraron documentos relevantes.");

        var result = await agent.ExecuteAsync(llm.Object, "Tesla", new(), CancellationToken.None);

        Assert.Contains("No se encontraron documentos", result.Output);
    }

    [Fact]
    public async Task FinancialDataExtractorAgent_WithDoc_ExtractsMetrics()
    {
        var doc = UploadedDocument.Create("company", Guid.NewGuid(), "tesla_financials.csv", "csv", "/tmp/tesla.csv", 2000);
        var sp = BuildServiceProvider(docs: new List<UploadedDocument> { doc });
        var agent = new FinancialDataExtractorAgent(sp);
        var llm = MockLlm("[{\"company\":\"Tesla\",\"metric\":\"Revenue\",\"value\":96773000000,\"currency\":\"USD\",\"year\":2025,\"quarter\":null,\"confidence\":\"high\"}]\n\n## Resumen\n- Revenue: $96.77B");

        var result = await agent.ExecuteAsync(llm.Object, "tesla", new(), CancellationToken.None);

        Assert.Contains("Revenue", result.Output);
        Assert.Contains("tesla_financials.csv", result.Sources);
        llm.Verify(l => l.ChatAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("tesla")), It.IsAny<LlmOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FinancialDataExtractorAgent_IncludesCompanyList()
    {
        var company = Company.Create("Apple Inc", ticker: "AAPL", country: "US", sector: "Technology");
        var doc = UploadedDocument.Create("Company", null, "apple_report.pdf", "pdf", "/docs/apple.pdf", 1024);
        var sp = BuildServiceProvider(companies: new List<Company> { company }, docs: new List<UploadedDocument> { doc });
        var agent = new FinancialDataExtractorAgent(sp);
        var llm = MockLlm("Datos extraídos correctamente.");

        var result = await agent.ExecuteAsync(llm.Object, "apple", new(), CancellationToken.None);

        // LLM should receive company context
        llm.Verify(l => l.ChatAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("Apple Inc")), It.IsAny<LlmOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FinancialDataExtractorAgent_HasCorrectMetadata()
    {
        var sp = BuildServiceProvider();
        var agent = new FinancialDataExtractorAgent(sp);

        Assert.Equal("FinancialDataExtractorAgent", agent.Name);
        Assert.Contains("Extracción", agent.Description);
    }

    // ── RiskAgent Tests ────────────────────────────────────────────────────

    [Fact]
    public async Task RiskAgent_NoData_ReportsEmpty()
    {
        var sp = BuildServiceProvider();
        var agent = new RiskAgent(sp);
        var llm = MockLlm("No se encontraron fondos ni empresas.");

        var result = await agent.ExecuteAsync(llm.Object, "Desconocido", new(), CancellationToken.None);

        Assert.Contains("No se encontraron", result.Output);
    }

    [Fact]
    public async Task RiskAgent_WithFunds_AnalyzesRisk()
    {
        var fund = Fund.Create(
            new ISIN("IE00B4L5Y983"),
            "iShares Core MSCI World",
            "Renta Variable Global",
            "BlackRock",
            RiskLevel.Medium,
            new Money(80.5m, "EUR"),
            new Percentage(0.20m));
        var perf = FundPerformance.Create(
            fund.Id,
            new Percentage(2.1m), new Percentage(5.5m), new Percentage(9.2m),
            new Percentage(15.3m), new Percentage(38.0m), new Percentage(55.0m),
            new Percentage(14.2m), 1.08m);
        fund.AddPerformance(perf);

        var sp = BuildServiceProvider(funds: new List<Fund> { fund });
        var agent = new RiskAgent(sp);
        var llm = MockLlm("## Perfil de riesgo: 🟠 Medio (5/7)\n- Volatilidad: 14.2%\n- Sharpe: 1.08");

        var result = await agent.ExecuteAsync(llm.Object, "MSCI World", new(), CancellationToken.None);

        Assert.Contains("riesgo", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("iShares Core MSCI World", result.Sources);
    }

    [Fact]
    public async Task RiskAgent_WithCompany_IncludesMetrics()
    {
        var company = Company.Create("Nvidia Corp", ticker: "NVDA", country: "US", sector: "Semiconductors");
        var metric = FinancialMetric.Create(
            entityType: "Company",
            entityId: company.Id,
            metricName: "Revenue",
            value: 60922000000m,
            currency: "USD",
            year: 2025,
            source: "annual_report",
            fileName: "nvidia_2025.pdf",
            confidence: "high");

        var sp = BuildServiceProvider(companies: new List<Company> { company }, metrics: new List<FinancialMetric> { metric });
        var agent = new RiskAgent(sp);
        var llm = MockLlm("## Nvidia - Riesgo Alto\nConcentración en sector GPU/AI.");

        var result = await agent.ExecuteAsync(llm.Object, "Nvidia", new(), CancellationToken.None);

        Assert.Contains("Nvidia", result.Output);
        Assert.Contains("Nvidia Corp (NVDA)", result.Sources);
        llm.Verify(l => l.ChatAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("Revenue")), It.IsAny<LlmOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RiskAgent_HasCorrectMetadata()
    {
        var sp = BuildServiceProvider();
        var agent = new RiskAgent(sp);

        Assert.Equal("RiskAgent", agent.Name);
        Assert.Contains("riesgo", agent.Description, StringComparison.OrdinalIgnoreCase);
    }
}
