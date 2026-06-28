using System.Text.Json;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using EconomIA.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EconomIA.Infrastructure.ExternalServices;

/// <summary>
/// Real market data provider using Yahoo Finance public API.
/// Fetches top ETFs/funds by predefined ISIN list (no auth required).
/// Falls back to SimulatedMarketDataProvider if Yahoo is unreachable.
/// </summary>
public class YahooFinanceProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YahooFinanceProvider> _logger;

    // Top European/global funds & ETFs with Yahoo tickers
    private static readonly (string Isin, string Ticker, string Name, string Category, string Company)[] KnownFunds =
    [
        ("IE00B4L5Y983", "IWDA.AS", "iShares Core MSCI World", "Renta Variable Global", "BlackRock"),
        ("IE00B5BMR087", "CSPX.L", "iShares Core S&P 500", "Indexado S&P500", "BlackRock"),
        ("IE00BKM4GZ66", "EMIM.L", "iShares Core MSCI EM IMI", "Emergentes", "BlackRock"),
        ("IE00B4L5YC18", "IMAE.L", "iShares MSCI Europe", "Europa", "BlackRock"),
        ("LU0392494562", "XDWD.DE", "Xtrackers MSCI World", "Renta Variable Global", "DWS"),
        ("LU0274208692", "XDWL.DE", "Xtrackers MSCI World Swap", "Renta Variable Global", "DWS"),
        ("IE00B52MJY50", "VHYL.L", "Vanguard FTSE All-World HDiv", "Renta Variable Global", "Vanguard"),
        ("IE00B3RBWM25", "VWRL.L", "Vanguard FTSE All-World", "Renta Variable Global", "Vanguard"),
        ("IE00BZ163G84", "VUSA.L", "Vanguard S&P 500", "Indexado S&P500", "Vanguard"),
        ("LU1681043599", "CW8.PA", "Amundi MSCI World", "Renta Variable Global", "Amundi"),
        ("IE00B1XNHC34", "IHYG.L", "iShares EUR HY Corp Bond", "Renta Fija", "BlackRock"),
        ("IE00B3F81R35", "IEGA.L", "iShares Core EUR Govt Bond", "Renta Fija", "BlackRock"),
        ("LU0996182563", "AEEM.PA", "Amundi MSCI Emerging Markets", "Emergentes", "Amundi"),
        ("IE00B4L5YX21", "SJPA.L", "iShares Core MSCI Japan IMI", "Asia-Pacífico", "BlackRock"),
        ("IE00B52VJ196", "ISAC.L", "iShares MSCI ACWI", "Renta Variable Global", "BlackRock"),
        ("LU1681048804", "PANX.PA", "Amundi Nasdaq-100", "Tecnología", "Amundi"),
        ("IE00BFMXXD54", "VWRP.L", "Vanguard FTSE All-World Acc", "Renta Variable Global", "Vanguard"),
        ("IE00B6R52259", "ISPA.L", "iShares MSCI ACWI SRI", "ESG", "BlackRock"),
        ("LU0908500753", "LYMS.DE", "Amundi S&P 500 ESG", "ESG", "Amundi"),
        ("IE00BJ0KDQ92", "XDWH.DE", "Xtrackers MSCI World ESG", "ESG", "DWS"),
        ("IE00B0M63177", "IEEM.L", "iShares MSCI EM", "Emergentes", "BlackRock"),
        ("IE00BKX55T58", "VWCE.DE", "Vanguard FTSE All-World Acc", "Renta Variable Global", "Vanguard"),
        ("LU1829220216", "MWRD.PA", "Amundi MSCI World SRI", "ESG", "Amundi"),
        ("IE00B3WJKG14", "VHYG.L", "iShares USD HY Corp Bond", "Renta Fija", "BlackRock"),
        ("LU2089238203", "CLIM.PA", "Amundi MSCI World Climate", "ESG", "Amundi"),
        ("IE00B3XXRP09", "VUSA.AS", "Vanguard S&P 500 EUR", "Indexado S&P500", "Vanguard"),
        ("IE00BZ02LR44", "QDVE.DE", "iShares S&P500 Info Tech", "Tecnología", "BlackRock"),
        ("IE00B4JNQZ49", "RBOT.L", "iShares Automation & Robotics", "Tecnología", "BlackRock"),
        ("LU0635178014", "CBEU5.DE", "ComStage EUR Corp Bond", "Renta Fija", "Lyxor"),
        ("IE00B1FZS574", "IDTL.L", "iShares EUR Govt Bond 15-30Y", "Renta Fija", "BlackRock"),
    ];

    public YahooFinanceProvider(HttpClient httpClient, ILogger<YahooFinanceProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Fund>> FetchTopFundsAsync(int count, CancellationToken ct = default)
    {
        var funds = new List<Fund>();
        var tickers = KnownFunds.Take(Math.Min(count, KnownFunds.Length)).ToArray();

        // Batch tickers in groups of 10 to avoid overloading
        var batches = tickers.Chunk(10);

        foreach (var batch in batches)
        {
            ct.ThrowIfCancellationRequested();
            var symbols = string.Join(",", batch.Select(b => b.Ticker));

            try
            {
                var url = $"https://query1.finance.yahoo.com/v7/finance/quote?symbols={Uri.EscapeDataString(symbols)}";
                var response = await _httpClient.GetAsync(url, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);

                var results = doc.RootElement
                    .GetProperty("quoteResponse")
                    .GetProperty("result");

                foreach (var quote in results.EnumerateArray())
                {
                    var symbol = quote.GetProperty("symbol").GetString() ?? "";
                    var known = batch.FirstOrDefault(b => b.Ticker == symbol);
                    if (known == default) continue;

                    var fund = MapQuoteToFund(quote, known);
                    if (fund != null) funds.Add(fund);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Yahoo Finance batch failed for {Symbols}", symbols);
            }

            // Small delay between batches to be polite
            if (batches.Count() > 1)
                await Task.Delay(200, ct);
        }

        _logger.LogInformation("Yahoo Finance: fetched {Count}/{Requested} funds", funds.Count, count);
        return funds;
    }

    public async Task<Fund?> FetchFundByIsinAsync(string isin, CancellationToken ct = default)
    {
        var known = KnownFunds.FirstOrDefault(f => f.Isin == isin);
        if (known == default)
        {
            _logger.LogDebug("ISIN {Isin} not in known funds list", isin);
            return null;
        }

        try
        {
            var url = $"https://query1.finance.yahoo.com/v7/finance/quote?symbols={Uri.EscapeDataString(known.Ticker)}";
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var results = doc.RootElement
                .GetProperty("quoteResponse")
                .GetProperty("result");

            if (results.GetArrayLength() == 0) return null;

            return MapQuoteToFund(results[0], known);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Yahoo Finance fetch failed for {Isin}", isin);
            return null;
        }
    }

    public async Task<FundPerformance?> FetchPerformanceAsync(Guid fundId, string isin, CancellationToken ct = default)
    {
        var known = KnownFunds.FirstOrDefault(f => f.Isin == isin);
        if (known == default) return null;

        try
        {
            // Use chart API for historical data (1 year range, monthly interval)
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(known.Ticker)}?range=5y&interval=1mo";
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var chart = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
            var closes = chart.GetProperty("indicators").GetProperty("adjclose")[0].GetProperty("adjclose");

            var prices = new List<decimal>();
            foreach (var price in closes.EnumerateArray())
            {
                if (price.ValueKind == JsonValueKind.Number)
                    prices.Add(price.GetDecimal());
            }

            if (prices.Count < 2) return null;

            var perf = CalculatePerformance(fundId, prices);
            return perf;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Yahoo Finance performance fetch failed for {Isin}", isin);
            return null;
        }
    }

    private static Fund? MapQuoteToFund(
        JsonElement quote,
        (string Isin, string Ticker, string Name, string Category, string Company) known)
    {
        try
        {
            var price = quote.TryGetProperty("regularMarketPrice", out var p) ? p.GetDecimal() : 0m;
            var currency = quote.TryGetProperty("currency", out var c) ? c.GetString() ?? "EUR" : "EUR";

            // Map risk level from 52-week volatility
            decimal fiftyTwoWeekRange = 0;
            if (quote.TryGetProperty("fiftyTwoWeekHigh", out var high) &&
                quote.TryGetProperty("fiftyTwoWeekLow", out var low) &&
                high.GetDecimal() > 0)
            {
                fiftyTwoWeekRange = (high.GetDecimal() - low.GetDecimal()) / high.GetDecimal() * 100;
            }

            var riskLevel = fiftyTwoWeekRange switch
            {
                < 5 => RiskLevel.VeryLow,
                < 10 => RiskLevel.Low,
                < 15 => RiskLevel.MediumLow,
                < 20 => RiskLevel.Medium,
                < 30 => RiskLevel.MediumHigh,
                < 40 => RiskLevel.High,
                _ => RiskLevel.VeryHigh,
            };

            // Expense ratio from Yahoo (often not available for ETFs via quote endpoint)
            var expense = quote.TryGetProperty("annualReportExpenseRatio", out var er)
                ? new Percentage(er.GetDecimal() * 100)
                : new Percentage(0.20m); // Default for passive ETFs

            return Fund.Create(
                new ISIN(known.Isin),
                known.Name,
                known.Category,
                known.Company,
                riskLevel,
                new Money(price, currency),
                expense);
        }
        catch
        {
            return null;
        }
    }

    private static FundPerformance CalculatePerformance(Guid fundId, List<decimal> monthlyPrices)
    {
        var count = monthlyPrices.Count;
        var current = monthlyPrices[^1];

        decimal Ret(int monthsBack) =>
            count > monthsBack && monthlyPrices[count - 1 - monthsBack] != 0
                ? Math.Round((current - monthlyPrices[count - 1 - monthsBack]) / monthlyPrices[count - 1 - monthsBack] * 100, 2)
                : 0;

        var return1m = Ret(1);
        var return3m = Ret(3);
        var return6m = Ret(6);
        var return1y = Ret(12);
        var return3y = Ret(36);
        var return5y = Ret(60);

        // Calculate monthly returns for volatility
        var monthlyReturns = new List<decimal>();
        for (int i = 1; i < Math.Min(count, 13); i++)
        {
            if (monthlyPrices[count - 1 - i] != 0)
                monthlyReturns.Add((monthlyPrices[count - i] - monthlyPrices[count - 1 - i]) / monthlyPrices[count - 1 - i]);
        }

        decimal volatility = 0;
        decimal sharpe = 0;
        if (monthlyReturns.Count > 1)
        {
            var avg = monthlyReturns.Average();
            var variance = monthlyReturns.Sum(r => (r - avg) * (r - avg)) / (monthlyReturns.Count - 1);
            volatility = Math.Round((decimal)Math.Sqrt((double)variance) * (decimal)Math.Sqrt(12) * 100, 2); // Annualized
            var riskFreeRate = 0.03m; // ~3% annual
            sharpe = volatility != 0
                ? Math.Round((return1y / 100 - riskFreeRate) / (volatility / 100), 2)
                : 0;
        }

        return FundPerformance.Create(
            fundId,
            new Percentage(return1m),
            new Percentage(return3m),
            new Percentage(return6m),
            new Percentage(return1y),
            new Percentage(return3y),
            new Percentage(return5y),
            new Percentage(volatility),
            sharpe);
    }
}
