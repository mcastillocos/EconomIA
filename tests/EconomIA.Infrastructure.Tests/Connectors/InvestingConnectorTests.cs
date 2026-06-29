using System.Text;
using EconomIA.Infrastructure.Connectors;

namespace EconomIA.Infrastructure.Tests.Connectors;

public class InvestingConnectorTests
{
    private readonly InvestingConnector _connector = new();

    [Fact]
    public async Task ExtractAsync_HistoricalPricesCsv_ShouldExtractOHLC()
    {
        var csv = """
            "Date","Price","Open","High","Low","Vol.","Change %"
            "Jun 28, 2026","185.50","183.20","186.10","182.90","12.5M","1.25%"
            "Jun 27, 2026","183.21","181.00","184.50","180.50","15.2M","0.88%"
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "AAPL Historical Data.csv", Ticker = "AAPL", EntityName = "Apple" };

        var results = await _connector.ExtractAsync(stream, metadata);

        // 2 filas × 5 métricas (price, open, high, low, volume + change_pct) = ~12
        Assert.True(results.Count >= 8);
        Assert.All(results, r => Assert.Equal("investing.com", r.Source));
        Assert.All(results, r => Assert.Equal("high", r.Confidence));
        Assert.Contains(results, r => r.Metric == "price" && r.Value == 185.50m);
        Assert.Contains(results, r => r.Metric == "high" && r.Value == 186.10m);
    }

    [Fact]
    public async Task ExtractAsync_SpanishFormat_ShouldParse()
    {
        var csv = """
            "Fecha","Último","Apertura","Máximo","Mínimo","Vol.","% var."
            "28/06/2026","185.50","183.20","186.10","182.90","12.5M","1.25%"
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "datos.csv" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.True(results.Count >= 4);
        Assert.Contains(results, r => r.Metric == "price" && r.Value == 185.50m);
        Assert.Contains(results, r => r.Metric == "open" && r.Value == 183.20m);
    }

    [Fact]
    public async Task ExtractAsync_VolumeWithSuffix_ShouldParseMK()
    {
        var csv = """
            "Date","Price","Vol."
            "Jun 28, 2026","100.00","1.5M"
            "Jun 27, 2026","99.50","850K"
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "test.csv" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.Contains(results, r => r.Metric == "volume" && r.Value == 1_500_000m);
        Assert.Contains(results, r => r.Metric == "volume" && r.Value == 850_000m);
    }

    [Fact]
    public async Task ExtractAsync_NoDateColumn_ShouldReturnEmpty()
    {
        var csv = """
            "Metric","Value"
            "Revenue","1000000"
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "nodate.csv" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.Empty(results); // No tiene columna Date, no es un export de Investing
    }

    [Fact]
    public async Task ExtractAsync_EmptyFile_ShouldReturnEmpty()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        var metadata = new ConnectorMetadata { FileName = "empty.csv" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.Empty(results);
    }
}
