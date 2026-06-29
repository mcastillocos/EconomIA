using EconomIA.Infrastructure.Connectors;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EconomIA.Infrastructure.Tests.Connectors;

public class FmpConnectorTests
{
    [Fact]
    public async Task ExtractAsync_ShouldReturnEmpty_BecauseItIsApiBasedNotFileBased()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FMP:ApiKey"] = "" })
            .Build();
        var connector = new FmpConnector(new HttpClient(), config);

        using var stream = new MemoryStream();
        var metadata = new ConnectorMetadata { FileName = "test.csv" };

        var results = await connector.ExtractAsync(stream, metadata);

        Assert.Empty(results); // FMP es API-based, no file-based
    }

    [Fact]
    public async Task FetchCompanyDataAsync_WithoutApiKey_ShouldReturnEmpty()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FMP:ApiKey"] = "" })
            .Build();
        var connector = new FmpConnector(new HttpClient(), config);

        var results = await connector.FetchCompanyDataAsync("AAPL");

        Assert.Empty(results);
    }

    [Fact]
    public async Task FetchQuoteAsync_WithoutApiKey_ShouldReturnNull()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FMP:ApiKey"] = "" })
            .Build();
        var connector = new FmpConnector(new HttpClient(), config);

        var quote = await connector.FetchQuoteAsync("AAPL");

        Assert.Null(quote);
    }

    [Fact]
    public void ConnectorName_ShouldBeFmpConnector()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FMP:ApiKey"] = "test" })
            .Build();
        var connector = new FmpConnector(new HttpClient(), config);

        Assert.Equal("fmp_connector", connector.ConnectorName);
    }
}
