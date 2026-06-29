using EconomIA.Infrastructure.Connectors;

namespace EconomIA.Infrastructure.Tests;

public class AudioConnectorParsingTests
{
    [Fact]
    public void ParseMetricsJson_ValidArray_ShouldExtractMetrics()
    {
        var json = """
            [
              {"metric": "Revenue", "value": 94.8, "period": "Q3 2024", "year": 2024, "quarter": 3, "currency": "USD", "confidence": "high", "rawText": "revenue of $94.8 billion"},
              {"metric": "EPS", "value": 1.40, "period": "Q3 2024", "year": 2024, "quarter": 3, "currency": "USD", "confidence": "high", "rawText": "EPS was $1.40"}
            ]
            """;

        var metadata = new ConnectorMetadata
        {
            FileName = "aapl_q3_2024.mp3",
            EntityType = "company",
            EntityName = "Apple",
            Ticker = "AAPL",
        };

        var results = AudioConnectorReal.ParseMetricsJson(json, metadata);

        Assert.Equal(2, results.Count);
        Assert.Equal("Revenue", results[0].Metric);
        Assert.Equal(94.8m, results[0].Value);
        Assert.Equal("Q3 2024", results[0].Period);
        Assert.Equal(2024, results[0].Year);
        Assert.Equal(3, results[0].Quarter);
        Assert.Equal("USD", results[0].Currency);
        Assert.Equal("high", results[0].Confidence);
        Assert.Equal("Apple", results[0].EntityName);
        Assert.Equal("AAPL", results[0].Ticker);

        Assert.Equal("EPS", results[1].Metric);
        Assert.Equal(1.40m, results[1].Value);
    }

    [Fact]
    public void ParseMetricsJson_WithSurroundingText_ShouldStillParse()
    {
        var json = """
            Here are the extracted metrics:
            [{"metric": "Net Income", "value": 23.6, "year": 2024, "quarter": 3, "currency": "USD", "confidence": "medium"}]
            Hope this helps!
            """;

        var metadata = new ConnectorMetadata { FileName = "test.mp3", EntityName = "Test" };
        var results = AudioConnectorReal.ParseMetricsJson(json, metadata);

        Assert.Single(results);
        Assert.Equal("Net Income", results[0].Metric);
        Assert.Equal(23.6m, results[0].Value);
    }

    [Fact]
    public void ParseMetricsJson_EmptyArray_ShouldReturnEmpty()
    {
        var json = "[]";
        var metadata = new ConnectorMetadata { FileName = "test.mp3" };

        var results = AudioConnectorReal.ParseMetricsJson(json, metadata);

        Assert.Empty(results);
    }

    [Fact]
    public void ParseMetricsJson_InvalidJson_ShouldReturnEmpty()
    {
        var json = "This is not JSON at all";
        var metadata = new ConnectorMetadata { FileName = "test.mp3" };

        var results = AudioConnectorReal.ParseMetricsJson(json, metadata);

        Assert.Empty(results);
    }

    [Fact]
    public void ParseMetricsJson_MissingMetricField_ShouldSkipItem()
    {
        var json = """[{"value": 100, "year": 2024}]""";
        var metadata = new ConnectorMetadata { FileName = "test.mp3" };

        var results = AudioConnectorReal.ParseMetricsJson(json, metadata);

        Assert.Empty(results); // metric vacío, se salta
    }

    [Fact]
    public void ParseMetricsJson_SetsSourceType()
    {
        var json = """[{"metric": "Revenue", "value": 50.0}]""";
        var metadata = new ConnectorMetadata { FileName = "call.mp3", Source = "earnings_call", EntityType = "company" };

        var results = AudioConnectorReal.ParseMetricsJson(json, metadata);

        Assert.Single(results);
        Assert.Equal("audio", results[0].SourceType);
        Assert.Equal("earnings_call", results[0].Source);
        Assert.Equal("company", results[0].EntityType);
    }
}
