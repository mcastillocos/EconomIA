using System.ServiceModel.Syndication;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace EconomIA.Infrastructure.Connectors;

/// <summary>
/// Conector real de noticias financieras via RSS/Atom feeds.
/// Soporta múltiples fuentes: Investing.com, Reuters, Bloomberg, Financial Times, etc.
/// </summary>
public class RssNewsConnector : IDataConnector
{
    public string ConnectorName => "news_connector";
    public string[] SupportedFileTypes => ["xml", "rss", "atom"];

    private readonly HttpClient _httpClient;
    private readonly ILogger<RssNewsConnector>? _logger;

    // Feeds RSS financieros públicos (fuentes que no bloquean)
    private static readonly string[] DefaultFeeds =
    [
        "https://www.cnbc.com/id/100003114/device/rss/rss.html", // CNBC Finance
        "https://feeds.bbci.co.uk/news/business/rss.xml",        // BBC Business
        "https://rss.nytimes.com/services/xml/rss/nyt/Business.xml", // NYT Business
        "https://seekingalpha.com/market_currents.xml",           // Seeking Alpha
        "https://finance.yahoo.com/news/rssindex",               // Yahoo Finance
        "https://www.marketwatch.com/rss/topstories",            // MarketWatch
        "https://www.economist.com/finance-and-economics/rss.xml", // The Economist
        "https://www.bloomberg.com/feed/podcast/etf-report.xml", // Bloomberg ETF
    ];

    public RssNewsConnector(HttpClient httpClient, ILogger<RssNewsConnector>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Extrae noticias de un stream XML/RSS proporcionado.
    /// </summary>
    public async Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        var results = new List<NormalizedDataPoint>();

        try
        {
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Ignore });
            var feed = SyndicationFeed.Load(reader);

            if (feed is null)
                return results;

            foreach (var item in feed.Items.Take(50)) // Limitamos a 50 noticias por feed
            {
                ct.ThrowIfCancellationRequested();
                results.Add(MapToDataPoint(item, feed.Title?.Text ?? metadata.Source ?? "rss"));
            }
        }
        catch (XmlException ex)
        {
            _logger?.LogWarning(ex, "Error parseando feed RSS desde '{Source}'", metadata.Source);
        }

        return results;
    }

    /// <summary>
    /// Obtiene noticias de los feeds RSS predeterminados.
    /// </summary>
    public async Task<IReadOnlyList<NewsItem>> FetchLatestNewsAsync(
        IReadOnlyList<string>? customFeeds = null,
        int maxPerFeed = 20,
        CancellationToken ct = default)
    {
        var feeds = customFeeds ?? DefaultFeeds;
        var allNews = new List<NewsItem>();

        var tasks = feeds.Select(async feedUrl =>
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, feedUrl);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 EconomIA/2.1");
                request.Headers.Add("Accept", "application/rss+xml, application/xml, text/xml, */*");
                using var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning("Feed {Url} devolvió {Status}", feedUrl, response.StatusCode);
                    return Enumerable.Empty<NewsItem>();
                }

                using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Ignore });
                var feed = SyndicationFeed.Load(reader);

                if (feed is null)
                    return Enumerable.Empty<NewsItem>();

                return feed.Items.Take(maxPerFeed).Select(item => new NewsItem
                {
                    Title = item.Title?.Text ?? "",
                    Summary = item.Summary?.Text ?? "",
                    Url = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "",
                    PublishedAt = item.PublishDate.UtcDateTime,
                    Source = feed.Title?.Text ?? new Uri(feedUrl).Host,
                    Categories = item.Categories.Select(c => c.Name).ToList(),
                });
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error obteniendo feed {Url}", feedUrl);
                return Enumerable.Empty<NewsItem>();
            }
        });

        var results = await Task.WhenAll(tasks);
        foreach (var batch in results)
            allNews.AddRange(batch);

        return allNews
            .OrderByDescending(n => n.PublishedAt)
            .Take(100)
            .ToList();
    }

    /// <summary>
    /// Filtra noticias relevantes por tickers/empresas del watchlist del usuario.
    /// </summary>
    public IReadOnlyList<NewsItem> FilterByRelevance(
        IReadOnlyList<NewsItem> news,
        IReadOnlyList<string> watchlistTerms)
    {
        if (watchlistTerms.Count == 0)
            return news;

        return news
            .Where(n => watchlistTerms.Any(term =>
                n.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                n.Summary.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                n.Categories.Any(c => c.Contains(term, StringComparison.OrdinalIgnoreCase))))
            .ToList();
    }

    private static NormalizedDataPoint MapToDataPoint(SyndicationItem item, string source)
    {
        return new NormalizedDataPoint
        {
            Source = source,
            SourceType = "news",
            EntityType = "market",
            EntityName = item.Title?.Text ?? "Unknown",
            Metric = "news_item",
            Value = 0, // Noticias no tienen valor numérico directo
            Url = item.Links.FirstOrDefault()?.Uri?.ToString(),
            RetrievedAt = DateTime.UtcNow,
            Confidence = "medium",
            RawText = item.Summary?.Text,
            RawReference = item.Id,
        };
    }
}

public record NewsItem
{
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public DateTime PublishedAt { get; init; }
    public string Source { get; init; } = string.Empty;
    public List<string> Categories { get; init; } = [];
}
