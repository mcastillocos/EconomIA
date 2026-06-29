using System.Text;
using EconomIA.Infrastructure.Connectors;
using Microsoft.Extensions.Logging;
using Moq;

namespace EconomIA.Infrastructure.Tests.Connectors;

public class RssNewsConnectorTests
{
    [Fact]
    public async Task ExtractAsync_ValidAtomFeed_ShouldExtractItems()
    {
        var atomXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <feed xmlns="http://www.w3.org/2005/Atom">
              <title>Financial News</title>
              <entry>
                <title>Apple reports record Q4 earnings</title>
                <summary>Apple Inc reported revenue of $94.9B</summary>
                <link href="https://example.com/apple-q4" />
                <id>urn:uuid:1234</id>
                <updated>2026-06-28T10:00:00Z</updated>
              </entry>
              <entry>
                <title>Fed maintains rates unchanged</title>
                <summary>The Federal Reserve held interest rates steady</summary>
                <link href="https://example.com/fed-rates" />
                <id>urn:uuid:5678</id>
                <updated>2026-06-28T09:00:00Z</updated>
              </entry>
            </feed>
            """;

        var connector = new RssNewsConnector(new HttpClient(), null);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(atomXml));
        var metadata = new ConnectorMetadata { FileName = "feed.xml", Source = "test_feed" };

        var results = await connector.ExtractAsync(stream, metadata);

        Assert.Equal(2, results.Count);
        Assert.Equal("news", results[0].SourceType);
        Assert.Equal("Apple reports record Q4 earnings", results[0].EntityName);
        Assert.Equal("news_item", results[0].Metric);
        Assert.Contains("Apple Inc reported", results[0].RawText!);
    }

    [Fact]
    public async Task ExtractAsync_ValidRssFeed_ShouldExtractItems()
    {
        var rssXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <rss version="2.0">
              <channel>
                <title>Market News</title>
                <item>
                  <title>S&amp;P 500 closes at all-time high</title>
                  <description>The index gained 1.2% on strong tech earnings</description>
                  <link>https://example.com/sp500</link>
                  <pubDate>Sat, 28 Jun 2026 15:00:00 GMT</pubDate>
                </item>
              </channel>
            </rss>
            """;

        var connector = new RssNewsConnector(new HttpClient(), null);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rssXml));
        var metadata = new ConnectorMetadata { FileName = "news.rss", Source = "market_news" };

        var results = await connector.ExtractAsync(stream, metadata);

        Assert.Single(results);
        Assert.Equal("S&P 500 closes at all-time high", results[0].EntityName);
        Assert.Equal("market", results[0].EntityType);
    }

    [Fact]
    public async Task ExtractAsync_InvalidXml_ShouldReturnEmpty()
    {
        var connector = new RssNewsConnector(new HttpClient(), new Mock<ILogger<RssNewsConnector>>().Object);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not xml at all"));
        var metadata = new ConnectorMetadata { FileName = "bad.xml" };

        var results = await connector.ExtractAsync(stream, metadata);

        Assert.Empty(results);
    }

    [Fact]
    public void FilterByRelevance_WithMatchingTerms_ShouldFilterCorrectly()
    {
        var connector = new RssNewsConnector(new HttpClient(), null);
        var news = new List<NewsItem>
        {
            new() { Title = "Apple Q4 beats expectations", Summary = "Strong iPhone sales", PublishedAt = DateTime.UtcNow },
            new() { Title = "Oil prices rise", Summary = "Brent crude up 3%", PublishedAt = DateTime.UtcNow },
            new() { Title = "Microsoft acquires AI startup", Summary = "Deal valued at $2B", PublishedAt = DateTime.UtcNow },
        };

        var filtered = connector.FilterByRelevance(news, ["Apple", "Microsoft"]);

        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, n => n.Title.Contains("Apple"));
        Assert.Contains(filtered, n => n.Title.Contains("Microsoft"));
    }

    [Fact]
    public void FilterByRelevance_EmptyTerms_ShouldReturnAll()
    {
        var connector = new RssNewsConnector(new HttpClient(), null);
        var news = new List<NewsItem>
        {
            new() { Title = "News 1", Summary = "", PublishedAt = DateTime.UtcNow },
            new() { Title = "News 2", Summary = "", PublishedAt = DateTime.UtcNow },
        };

        var filtered = connector.FilterByRelevance(news, []);

        Assert.Equal(2, filtered.Count);
    }
}
