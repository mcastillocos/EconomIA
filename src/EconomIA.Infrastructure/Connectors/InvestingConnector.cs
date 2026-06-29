using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.ServiceModel.Syndication;
using Microsoft.Extensions.Logging;

namespace EconomIA.Infrastructure.Connectors;

/// <summary>
/// Conector real para Investing.com.
/// 1. Parsea exports CSV descargados (datos históricos de cotizaciones).
/// 2. Obtiene calendario económico y noticias via RSS.
/// 
/// Formato típico Investing.com CSV export:
///   "Date","Price","Open","High","Low","Vol.","Change %"
///   "06/28/2026","185.50","183.20","186.10","182.90","12.5M","1.25%"
/// </summary>
public partial class InvestingConnector : IDataConnector
{
    public string ConnectorName => "investing_connector";
    public string[] SupportedFileTypes => ["csv"];

    private readonly HttpClient? _httpClient;
    private readonly ILogger<InvestingConnector>? _logger;

    private static readonly string[] InvestingRssFeeds =
    [
        "https://www.investing.com/rss/news.rss",
        "https://www.investing.com/rss/economic_calendar.rss",
        "https://www.investing.com/rss/market_overview.rss",
    ];

    // Mapeo de headers de Investing.com exports (multi-idioma)
    private static readonly Dictionary<string, string> HeaderMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Date"] = "date", ["Fecha"] = "date",
        ["Price"] = "price", ["Precio"] = "price", ["Último"] = "price", ["Last"] = "price",
        ["Open"] = "open", ["Apertura"] = "open",
        ["High"] = "high", ["Máximo"] = "high", ["Max"] = "high",
        ["Low"] = "low", ["Mínimo"] = "low", ["Min"] = "low",
        ["Vol."] = "volume", ["Volume"] = "volume", ["Volumen"] = "volume",
        ["Change %"] = "change_pct", ["% var."] = "change_pct", ["Var. %"] = "change_pct",
        ["Change"] = "change", ["Var."] = "change",
    };

    public InvestingConnector() { }

    public InvestingConnector(HttpClient httpClient, ILogger<InvestingConnector>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Parsea un CSV exportado de Investing.com con datos históricos de cotizaciones.
    /// </summary>
    public async Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        var results = new List<NormalizedDataPoint>();

        using var reader = new StreamReader(stream);
        var headerLine = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(headerLine)) return results;

        var separator = headerLine.Contains('\t') ? '\t' : ',';
        var rawHeaders = ParseCsvLine(headerLine, separator);
        var headers = rawHeaders.Select(h => NormalizeHeader(h.Trim().Trim('"'))).ToArray();

        var dateIdx = Array.IndexOf(headers, "date");
        if (dateIdx < 0) return results; // No tiene columna de fecha, no es un export de Investing

        int row = 0;
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;
            row++;

            var values = ParseCsvLine(line, separator);
            if (values.Length != headers.Length) continue;

            var dateStr = values[dateIdx].Trim('"');
            var date = ParseInvestingDate(dateStr);

            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i] == "date") continue;
                if (string.IsNullOrWhiteSpace(headers[i])) continue;

                var rawValue = values[i].Trim().Trim('"');
                var numericValue = ParseInvestingNumber(rawValue);
                if (numericValue is null) continue;

                results.Add(new NormalizedDataPoint
                {
                    Source = "investing.com",
                    SourceType = "api",
                    EntityType = metadata.EntityType ?? "market",
                    EntityName = metadata.EntityName ?? Path.GetFileNameWithoutExtension(metadata.FileName),
                    Ticker = metadata.Ticker,
                    Isin = metadata.Isin,
                    Metric = headers[i],
                    Value = numericValue.Value,
                    Year = date?.Year,
                    Period = date?.ToString("yyyy-MM-dd"),
                    Currency = metadata.Source?.Contains("USD", StringComparison.OrdinalIgnoreCase) == true ? "USD" : null,
                    FileName = metadata.FileName,
                    Row = row.ToString(),
                    RetrievedAt = DateTime.UtcNow,
                    Confidence = "high",
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Obtiene eventos del calendario económico y noticias via RSS de Investing.com.
    /// </summary>
    public async Task<IReadOnlyList<EconomicEvent>> FetchEconomicCalendarAsync(CancellationToken ct = default)
    {
        if (_httpClient is null)
            return [];

        var events = new List<EconomicEvent>();

        foreach (var feedUrl in InvestingRssFeeds)
        {
            try
            {
                using var response = await _httpClient.GetAsync(feedUrl, ct);
                if (!response.IsSuccessStatusCode) continue;

                using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Ignore });
                var feed = SyndicationFeed.Load(reader);
                if (feed is null) continue;

                foreach (var item in feed.Items.Take(30))
                {
                    events.Add(new EconomicEvent
                    {
                        Title = item.Title?.Text ?? "",
                        Description = item.Summary?.Text ?? "",
                        Url = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "",
                        PublishedAt = item.PublishDate.UtcDateTime,
                        Source = "investing.com",
                        Category = item.Categories.FirstOrDefault()?.Name ?? "general",
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error obteniendo feed de Investing.com: {Url}", feedUrl);
            }
        }

        return events.OrderByDescending(e => e.PublishedAt).ToList();
    }

    private static string NormalizeHeader(string raw)
    {
        return HeaderMappings.TryGetValue(raw, out var mapped) ? mapped : raw.ToLowerInvariant();
    }

    private static DateTime? ParseInvestingDate(string dateStr)
    {
        // Investing.com usa formatos: "Jun 28, 2026", "06/28/2026", "28/06/2026"
        string[] formats = ["MMM dd, yyyy", "MM/dd/yyyy", "dd/MM/yyyy", "yyyy-MM-dd"];
        if (DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;
        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return dt;
        return null;
    }

    private static decimal? ParseInvestingNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw == "-" || raw == "N/A") return null;

        // Quitar % al final
        raw = raw.TrimEnd('%');

        // Manejar sufijos: 1.5K, 12.5M, 3.2B, 1.1T
        var multiplier = 1m;
        if (raw.EndsWith("K", StringComparison.OrdinalIgnoreCase)) { multiplier = 1_000m; raw = raw[..^1]; }
        else if (raw.EndsWith("M", StringComparison.OrdinalIgnoreCase)) { multiplier = 1_000_000m; raw = raw[..^1]; }
        else if (raw.EndsWith("B", StringComparison.OrdinalIgnoreCase)) { multiplier = 1_000_000_000m; raw = raw[..^1]; }
        else if (raw.EndsWith("T", StringComparison.OrdinalIgnoreCase)) { multiplier = 1_000_000_000_000m; raw = raw[..^1]; }

        raw = raw.Replace(",", "").Trim();

        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            return val * multiplier;

        return null;
    }

    private static string[] ParseCsvLine(string line, char separator)
    {
        if (separator != ',') return line.Split(separator);

        var parts = new List<string>();
        var inQuotes = false;
        var current = new System.Text.StringBuilder();

        foreach (var c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; current.Append(c); continue; }
            if (c == ',' && !inQuotes) { parts.Add(current.ToString()); current.Clear(); continue; }
            current.Append(c);
        }
        parts.Add(current.ToString());
        return parts.ToArray();
    }
}

public record EconomicEvent
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public DateTime PublishedAt { get; init; }
    public string Source { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
}
