using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using EconomIA.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EconomIA.Infrastructure.ExternalServices;

/// <summary>
/// Simulated market data provider. Replace with real API calls (Yahoo Finance, Morningstar, etc.)
/// </summary>
public class SimulatedMarketDataProvider : IMarketDataProvider
{
    private readonly ILogger<SimulatedMarketDataProvider> _logger;
    private static readonly Random _random = new();
    private static readonly string[] Categories = ["Renta Variable Global", "Renta Fija", "Mixto", "Tecnología", "Emergentes", "ESG", "Indexado S&P500", "Europa", "Asia-Pacífico", "Materias Primas"];
    private static readonly string[] Companies = ["BlackRock", "Vanguard", "Fidelity", "PIMCO", "Amundi", "Schroders", "JPMorgan AM", "Goldman Sachs AM", "Morgan Stanley IM", "DWS"];

    public SimulatedMarketDataProvider(ILogger<SimulatedMarketDataProvider> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<Fund>> FetchTopFundsAsync(int count, CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching top {Count} funds from market data", count);

        var funds = Enumerable.Range(1, count).Select(i =>
        {
            var isin = new ISIN($"{GetCountryCode()}{GenerateAlphanumeric(9)}{_random.Next(0, 10)}");
            var category = Categories[_random.Next(Categories.Length)];
            var company = Companies[_random.Next(Companies.Length)];
            var riskLevel = (RiskLevel)_random.Next(1, 8);
            var nav = new Money(Math.Round((decimal)(_random.NextDouble() * 500 + 10), 2), "EUR");
            var expense = new Percentage(Math.Round((decimal)(_random.NextDouble() * 2.5), 4));

            return Fund.Create(isin, $"{company} {category} Fund {i}", category, company, riskLevel, nav, expense);
        }).ToList();

        return Task.FromResult<IReadOnlyList<Fund>>(funds);
    }

    public Task<Fund?> FetchFundByIsinAsync(string isin, CancellationToken ct = default)
    {
        var fund = Fund.Create(
            new ISIN(isin),
            "Sample Fund",
            Categories[0],
            Companies[0],
            RiskLevel.Medium,
            new Money(100m, "EUR"),
            new Percentage(1.5m));

        return Task.FromResult<Fund?>(fund);
    }

    public Task<FundPerformance?> FetchPerformanceAsync(Guid fundId, string isin, CancellationToken ct = default)
    {
        var perf = FundPerformance.Create(
            fundId,
            new Percentage(Math.Round((decimal)(_random.NextDouble() * 10 - 3), 2)),
            new Percentage(Math.Round((decimal)(_random.NextDouble() * 15 - 5), 2)),
            new Percentage(Math.Round((decimal)(_random.NextDouble() * 20 - 5), 2)),
            new Percentage(Math.Round((decimal)(_random.NextDouble() * 30 - 5), 2)),
            new Percentage(Math.Round((decimal)(_random.NextDouble() * 50 - 10), 2)),
            new Percentage(Math.Round((decimal)(_random.NextDouble() * 80 - 10), 2)),
            new Percentage(Math.Round((decimal)(_random.NextDouble() * 25), 2)),
            Math.Round((decimal)(_random.NextDouble() * 3), 2));

        return Task.FromResult<FundPerformance?>(perf);
    }

    private static string GetCountryCode()
    {
        var codes = new[] { "ES", "LU", "IE", "FR", "DE", "GB", "US" };
        return codes[_random.Next(codes.Length)];
    }

    private static string GenerateAlphanumeric(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
    }
}
