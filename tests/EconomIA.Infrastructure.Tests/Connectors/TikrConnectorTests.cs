using System.Text;
using EconomIA.Infrastructure.Connectors;

namespace EconomIA.Infrastructure.Tests.Connectors;

public class TikrConnectorTests
{
    private readonly TikrConnector _connector = new();

    [Fact]
    public async Task ExtractAsync_TikrIncomeStatement_ShouldExtractMetrics()
    {
        var csv = "AAPL\n,FY 2022,FY 2023,FY 2024\nRevenue,\"394,328,000,000\",\"383,285,000,000\",\"391,035,000,000\"\nGross Profit,\"170,782,000,000\",\"169,148,000,000\",\"180,683,000,000\"\nOperating Income,\"119,437,000,000\",\"114,301,000,000\",\"123,216,000,000\"\nNet Income,\"99,803,000,000\",\"96,995,000,000\",\"93,736,000,000\"\nEBITDA,\"130,541,000,000\",\"125,820,000,000\",\"133,000,000,000\"\nEPS (Diluted),6.11,6.13,6.08";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "AAPL_income.csv", EntityType = "company" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.True(results.Count >= 12); // 6 métricas × 3 años (mínimo, algunas pueden no parsear)
        Assert.All(results, r => Assert.Equal("tikr.com", r.Source));
        Assert.All(results, r => Assert.Equal("high", r.Confidence));
        Assert.Contains(results, r => r.Metric == "Revenue" && r.Year == 2022);
        Assert.Contains(results, r => r.Metric == "NetIncome" && r.Year == 2024);
        Assert.Contains(results, r => r.Metric == "EPSDiluted");
    }

    [Fact]
    public async Task ExtractAsync_TikrBalanceSheet_ShouldExtractAssets()
    {
        var csv = "ITX\n,FY 2023,FY 2024\nTotal Assets,25000000000,27000000000\nTotal Equity,12000000000,13500000000\nTotal Debt,5000000000,4800000000\nCash and Equivalents,8000000000,9200000000";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "ITX_balance.csv", Ticker = "ITX" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.Contains(results, r => r.Metric == "TotalAssets" && r.Year == 2023 && r.Value == 25000000000m);
        Assert.Contains(results, r => r.Metric == "TotalEquity" && r.Year == 2024 && r.Value == 13500000000m);
        Assert.Contains(results, r => r.Metric == "CashAndEquivalents");
        Assert.All(results, r => Assert.Equal("ITX", r.Ticker));
    }

    [Fact]
    public async Task ExtractAsync_WithNegativeValues_ShouldParseParentheses()
    {
        var csv = "TSLA\n,FY 2020,FY 2021\nNet Income,(862000000),5519000000\nFree Cash Flow,(1440000000),5000000000";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "TSLA.csv" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.Contains(results, r => r.Metric == "NetIncome" && r.Year == 2020 && r.Value == -862000000m);
        Assert.Contains(results, r => r.Metric == "FreeCashFlow" && r.Year == 2020 && r.Value < 0);
    }

    [Fact]
    public async Task ExtractAsync_WithPercentages_ShouldParse()
    {
        var csv = "MSFT\n,FY 2023,FY 2024\nGross Margin,68.9%,69.4%\nOperating Margin,41.2%,44.6%\nROE,35.1%,37.8%";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "MSFT_ratios.csv" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.Contains(results, r => r.Metric == "GrossMargin" && r.Value == 68.9m);
        Assert.Contains(results, r => r.Metric == "OperatingMargin" && r.Year == 2024 && r.Value == 44.6m);
        Assert.Contains(results, r => r.Metric == "ROE");
    }

    [Fact]
    public async Task ExtractAsync_UnrecognizedMetrics_ShouldSkip()
    {
        var csv = "AAPL\n,FY 2024\nRevenue,391035000000\nSome Random Metric,12345\nAnother Unknown,67890\nEBITDA,133000000000";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "test.csv" };

        var results = await _connector.ExtractAsync(stream, metadata);

        // Solo Revenue y EBITDA deberían ser reconocidos
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Metric == "Revenue");
        Assert.Contains(results, r => r.Metric == "EBITDA");
    }

    [Fact]
    public async Task ExtractAsync_EmptyFile_ShouldReturnEmpty()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        var metadata = new ConnectorMetadata { FileName = "empty.csv" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ExtractAsync_DetectsTickerFromFirstRow()
    {
        var csv = "GOOG\n,FY 2024\nRevenue,350000000000";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "export.csv" };

        var results = await _connector.ExtractAsync(stream, metadata);

        Assert.Single(results);
        Assert.Equal("GOOG", results[0].Ticker);
    }
}
