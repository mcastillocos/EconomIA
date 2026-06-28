using System.Text;
using EconomIA.Infrastructure.Connectors;

namespace EconomIA.Infrastructure.Tests.Connectors;

public class CsvConnectorTests
{
    private readonly CsvConnector _connector = new();

    [Fact]
    public async Task ExtractAsync_WithMetricValueColumns_ShouldExtractData()
    {
        var csv = "ticker,metric,value,year,currency\nITX,Revenue,125000000,2024,EUR\nITX,EBITDA,20000000,2024,EUR";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "test.csv", EntityType = "company" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.Equal(2, results.Count);
        Assert.Equal("Revenue", results[0].Metric);
        Assert.Equal(125000000m, results[0].Value);
        Assert.Equal("ITX", results[0].Ticker);
        Assert.Equal(2024, results[0].Year);
        Assert.Equal("EUR", results[0].Currency);
        Assert.Equal("high", results[0].Confidence);
    }

    [Fact]
    public async Task ExtractAsync_WithNumericColumns_ShouldExtractEachAsMetric()
    {
        var csv = "name,revenue,ebitda,year\nInditex,125000,20000,2024";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "test.csv", EntityType = "company" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.Equal(2, results.Count); // revenue and ebitda (year is excluded as non-metric)
        Assert.Contains(results, r => r.Metric == "revenue" && r.Value == 125000m);
        Assert.Contains(results, r => r.Metric == "ebitda" && r.Value == 20000m);
    }

    [Fact]
    public async Task ExtractAsync_WithSemicolonSeparator_ShouldParse()
    {
        var csv = "ticker;metric;value\nITX;Revenue;100";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "test.csv" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.Single(results);
        Assert.Equal("Revenue", results[0].Metric);
        Assert.Equal(100m, results[0].Value);
    }

    [Fact]
    public async Task ExtractAsync_WithEmptyFile_ShouldReturnEmpty()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        var metadata = new ConnectorMetadata { FileName = "empty.csv" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ExtractAsync_WithHeaderOnly_ShouldReturnEmpty()
    {
        var csv = "ticker,metric,value";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "header_only.csv" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ExtractAsync_SourceType_ShouldBeCsv()
    {
        var csv = "metric,value\nRevenue,100";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "test.csv" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.All(results, r => Assert.Equal("csv", r.SourceType));
    }

    [Fact]
    public void ConnectorName_ShouldBeCsvConnector()
    {
        Assert.Equal("csv_connector", _connector.ConnectorName);
    }

    [Fact]
    public void SupportedFileTypes_ShouldContainCsvAndTsv()
    {
        Assert.Contains("csv", _connector.SupportedFileTypes);
        Assert.Contains("tsv", _connector.SupportedFileTypes);
    }
}
