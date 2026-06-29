using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EconomIA.Infrastructure.Connectors;

/// <summary>
/// Conector real para Financial Modeling Prep (financialmodelingprep.com).
/// API gratuita (250 req/día) para datos fundamentales: income statement, balance sheet, 
/// ratios, profile, quote, etc.
/// Requiere API key configurada en appsettings.json → FMP:ApiKey
/// </summary>
public class FmpConnector : IDataConnector
{
    public string ConnectorName => "fmp_connector";
    public string[] SupportedFileTypes => [];

    private readonly HttpClient _httpClient;
    private readonly ILogger<FmpConnector>? _logger;
    private readonly string _apiKey;
    private const string BaseUrl = "https://financialmodelingprep.com/api/v3";

    public FmpConnector(HttpClient httpClient, IConfiguration configuration, ILogger<FmpConnector>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["FMP:ApiKey"] ?? "";
    }

    /// <summary>
    /// No usado directamente — FMP es un conector API, no de archivos.
    /// </summary>
    public Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<NormalizedDataPoint>>(Array.Empty<NormalizedDataPoint>());
    }

    /// <summary>
    /// Obtiene datos fundamentales completos de una empresa por ticker.
    /// </summary>
    public async Task<IReadOnlyList<NormalizedDataPoint>> FetchCompanyDataAsync(string ticker, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger?.LogWarning("FMP API key no configurada. Configurar en appsettings FMP:ApiKey");
            return [];
        }

        var results = new List<NormalizedDataPoint>();

        // 1. Profile (datos básicos)
        var profile = await FetchJsonArrayAsync($"{BaseUrl}/profile/{ticker}?apikey={_apiKey}", ct);
        if (profile is { } profileArr && profileArr.GetArrayLength() > 0)
        {
            var p = profileArr[0];
            AddIfNumber(results, ticker, "MarketCap", p, "mktCap");
            AddIfNumber(results, ticker, "PERatio", p, "peRatio", source: "profile");
            AddIfNumber(results, ticker, "Beta", p, "beta");
            AddIfNumber(results, ticker, "DividendYield", p, "lastDiv");
            AddIfNumber(results, ticker, "Price", p, "price");
            AddIfString(results, ticker, "Sector", p, "sector");
            AddIfString(results, ticker, "Industry", p, "industry");
            AddIfString(results, ticker, "Country", p, "country");
            AddIfString(results, ticker, "Currency", p, "currency");
        }

        // 2. Income Statement (últimos 5 años)
        var income = await FetchJsonArrayAsync($"{BaseUrl}/income-statement/{ticker}?limit=5&apikey={_apiKey}", ct);
        if (income is { } incomeArr)
        {
            foreach (var item in incomeArr.EnumerateArray())
            {
                var year = GetYear(item);
                AddIfNumber(results, ticker, "Revenue", item, "revenue", year);
                AddIfNumber(results, ticker, "GrossProfit", item, "grossProfit", year);
                AddIfNumber(results, ticker, "OperatingIncome", item, "operatingIncome", year);
                AddIfNumber(results, ticker, "NetIncome", item, "netIncome", year);
                AddIfNumber(results, ticker, "EBITDA", item, "ebitda", year);
                AddIfNumber(results, ticker, "EPS", item, "eps", year);
                AddIfNumber(results, ticker, "EPSDiluted", item, "epsdiluted", year);
                AddIfNumber(results, ticker, "GrossMargin", item, "grossProfitRatio", year);
                AddIfNumber(results, ticker, "OperatingMargin", item, "operatingIncomeRatio", year);
                AddIfNumber(results, ticker, "NetMargin", item, "netIncomeRatio", year);
            }
        }

        // 3. Balance Sheet (últimos 5 años)
        var balance = await FetchJsonArrayAsync($"{BaseUrl}/balance-sheet-statement/{ticker}?limit=5&apikey={_apiKey}", ct);
        if (balance is { } balanceArr)
        {
            foreach (var item in balanceArr.EnumerateArray())
            {
                var year = GetYear(item);
                AddIfNumber(results, ticker, "TotalAssets", item, "totalAssets", year);
                AddIfNumber(results, ticker, "TotalLiabilities", item, "totalLiabilities", year);
                AddIfNumber(results, ticker, "TotalEquity", item, "totalStockholdersEquity", year);
                AddIfNumber(results, ticker, "TotalDebt", item, "totalDebt", year);
                AddIfNumber(results, ticker, "CashAndEquivalents", item, "cashAndCashEquivalents", year);
                AddIfNumber(results, ticker, "LongTermDebt", item, "longTermDebt", year);
            }
        }

        // 4. Key Ratios (últimos 5 años)
        var ratios = await FetchJsonArrayAsync($"{BaseUrl}/ratios/{ticker}?limit=5&apikey={_apiKey}", ct);
        if (ratios is { } ratiosArr)
        {
            foreach (var item in ratiosArr.EnumerateArray())
            {
                var year = GetYear(item);
                AddIfNumber(results, ticker, "ROE", item, "returnOnEquity", year);
                AddIfNumber(results, ticker, "ROA", item, "returnOnAssets", year);
                AddIfNumber(results, ticker, "ROIC", item, "returnOnCapitalEmployed", year);
                AddIfNumber(results, ticker, "CurrentRatio", item, "currentRatio", year);
                AddIfNumber(results, ticker, "DebtToEquity", item, "debtEquityRatio", year);
                AddIfNumber(results, ticker, "PERatio", item, "priceEarningsRatio", year, source: "ratios");
                AddIfNumber(results, ticker, "PBRatio", item, "priceToBookRatio", year);
                AddIfNumber(results, ticker, "EVToEBITDA", item, "enterpriseValueOverEBITDA", year);
                AddIfNumber(results, ticker, "DividendYield", item, "dividendYield", year);
                AddIfNumber(results, ticker, "PayoutRatio", item, "payoutRatio", year);
                AddIfNumber(results, ticker, "FreeCashFlowPerShare", item, "freeCashFlowPerShare", year);
            }
        }

        _logger?.LogInformation("FMP: Extraídos {Count} data points para {Ticker}", results.Count, ticker);
        return results;
    }

    /// <summary>
    /// Obtiene cotización en tiempo real.
    /// </summary>
    public async Task<FmpQuote?> FetchQuoteAsync(string ticker, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return null;

        var data = await FetchJsonArrayAsync($"{BaseUrl}/quote/{ticker}?apikey={_apiKey}", ct);
        if (data is not { } dataArr || dataArr.GetArrayLength() == 0) return null;

        var q = dataArr[0];
        return new FmpQuote
        {
            Ticker = ticker,
            Price = GetDecimal(q, "price"),
            Change = GetDecimal(q, "change"),
            ChangePct = GetDecimal(q, "changesPercentage"),
            DayHigh = GetDecimal(q, "dayHigh"),
            DayLow = GetDecimal(q, "dayLow"),
            Volume = GetDecimal(q, "volume"),
            MarketCap = GetDecimal(q, "marketCap"),
            PERatio = GetDecimal(q, "pe"),
            EPS = GetDecimal(q, "eps"),
        };
    }

    private async Task<JsonElement?> FetchJsonArrayAsync(string url, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("FMP API devolvió {Status} para {Url}", response.StatusCode, MaskApiKey(url));
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement : null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error llamando FMP API: {Url}", MaskApiKey(url));
            return null;
        }
    }

    private static void AddIfNumber(List<NormalizedDataPoint> results, string ticker, string metric,
        JsonElement element, string jsonProp, int? year = null, string source = "fmp")
    {
        if (!element.TryGetProperty(jsonProp, out var prop)) return;
        var value = prop.ValueKind switch
        {
            JsonValueKind.Number => prop.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) => v,
            _ => (decimal?)null,
        };

        if (value is null || value == 0) return;

        results.Add(new NormalizedDataPoint
        {
            Source = $"financialmodelingprep.com/{source}",
            SourceType = "api",
            EntityType = "company",
            EntityName = ticker,
            Ticker = ticker,
            Metric = metric,
            Value = value.Value,
            Year = year,
            RetrievedAt = DateTime.UtcNow,
            Confidence = "high",
        });
    }

    private static void AddIfString(List<NormalizedDataPoint> results, string ticker, string metric,
        JsonElement element, string jsonProp)
    {
        if (!element.TryGetProperty(jsonProp, out var prop) || prop.ValueKind != JsonValueKind.String) return;
        var strVal = prop.GetString();
        if (string.IsNullOrWhiteSpace(strVal)) return;

        results.Add(new NormalizedDataPoint
        {
            Source = "financialmodelingprep.com/profile",
            SourceType = "api",
            EntityType = "company",
            EntityName = ticker,
            Ticker = ticker,
            Metric = metric,
            Value = 0,
            RawText = strVal,
            RetrievedAt = DateTime.UtcNow,
            Confidence = "high",
        });
    }

    private static int? GetYear(JsonElement element)
    {
        if (element.TryGetProperty("calendarYear", out var cy) && cy.ValueKind == JsonValueKind.String &&
            int.TryParse(cy.GetString(), out var year))
            return year;

        if (element.TryGetProperty("date", out var dt) && dt.ValueKind == JsonValueKind.String &&
            DateTime.TryParse(dt.GetString(), out var date))
            return date.Year;

        return null;
    }

    private static decimal? GetDecimal(JsonElement element, string prop)
    {
        if (!element.TryGetProperty(prop, out var val)) return null;
        return val.ValueKind == JsonValueKind.Number ? val.GetDecimal() : null;
    }

    private static string MaskApiKey(string url)
    {
        var idx = url.IndexOf("apikey=", StringComparison.Ordinal);
        return idx >= 0 ? url[..(idx + 10)] + "***" : url;
    }
}

public record FmpQuote
{
    public string Ticker { get; init; } = string.Empty;
    public decimal? Price { get; init; }
    public decimal? Change { get; init; }
    public decimal? ChangePct { get; init; }
    public decimal? DayHigh { get; init; }
    public decimal? DayLow { get; init; }
    public decimal? Volume { get; init; }
    public decimal? MarketCap { get; init; }
    public decimal? PERatio { get; init; }
    public decimal? EPS { get; init; }
}
