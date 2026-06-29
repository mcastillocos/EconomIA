using System.Text;
using EconomIA.Infrastructure.Connectors;
using Microsoft.Extensions.Logging;
using Moq;

namespace EconomIA.Infrastructure.Tests.Connectors;

public class ConnectorOrchestratorTests
{
    private readonly ConnectorOrchestrator _orchestrator;
    private readonly ILogger<ConnectorOrchestrator> _logger = new Mock<ILogger<ConnectorOrchestrator>>().Object;

    public ConnectorOrchestratorTests()
    {
        var connectors = new IDataConnector[]
        {
            new CsvConnector(),
            new ExcelConnector(),
            new PdfConnector(),
            new TikrConnector(),
        };
        _orchestrator = new ConnectorOrchestrator(connectors, _logger);
    }

    [Fact]
    public async Task ProcessAsync_CsvFile_ShouldRouteToCsvConnector()
    {
        var csv = "ticker,metric,value\nAAPL,Revenue,394000000000";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "datos.csv", EntityType = "company" };

        var result = await _orchestrator.ProcessAsync(stream, metadata);

        Assert.True(result.IsSuccess);
        Assert.Equal("csv_connector", result.ConnectorUsed);
        Assert.Single(result.DataPoints);
        Assert.Equal("Revenue", result.DataPoints[0].Metric);
    }

    [Fact]
    public async Task ProcessAsync_UnknownExtension_ShouldReturnNoConnector()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var metadata = new ConnectorMetadata { FileName = "data.unknown" };

        var result = await _orchestrator.ProcessAsync(stream, metadata);

        Assert.False(result.IsSuccess);
        Assert.Equal("No hay conector disponible", result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessAsync_WithSourceHint_ShouldRouteBySource()
    {
        using var stream = new MemoryStream();
        var metadata = new ConnectorMetadata { FileName = "data.json", Source = "tikr" };

        var result = await _orchestrator.ProcessAsync(stream, metadata);

        // TikrConnector es un stub que devuelve vacío, pero se selecciona correctamente
        Assert.True(result.IsSuccess);
        Assert.Equal("tikr_connector", result.ConnectorUsed);
        Assert.Empty(result.DataPoints);
    }

    [Fact]
    public async Task ProcessWithConnectorAsync_ByName_ShouldUseSpecificConnector()
    {
        var csv = "metric,value\nPER,18.5";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var metadata = new ConnectorMetadata { FileName = "test.csv" };

        var result = await _orchestrator.ProcessWithConnectorAsync("csv_connector", stream, metadata);

        Assert.True(result.IsSuccess);
        Assert.Equal("csv_connector", result.ConnectorUsed);
    }

    [Fact]
    public async Task ProcessWithConnectorAsync_UnknownName_ShouldReturnNoConnector()
    {
        using var stream = new MemoryStream();
        var metadata = new ConnectorMetadata { FileName = "test.csv" };

        var result = await _orchestrator.ProcessWithConnectorAsync("nonexistent", stream, metadata);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ProcessBatchAsync_MultipleCsvFiles_ShouldProcessAll()
    {
        var items = new List<(Stream Stream, ConnectorMetadata Metadata)>
        {
            (new MemoryStream(Encoding.UTF8.GetBytes("metric,value\nROE,15.3")),
                new ConnectorMetadata { FileName = "a.csv" }),
            (new MemoryStream(Encoding.UTF8.GetBytes("metric,value\nPER,22.1")),
                new ConnectorMetadata { FileName = "b.csv" }),
        };

        var results = await _orchestrator.ProcessBatchAsync(items);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.IsSuccess));
    }

    [Fact]
    public void GetAvailableConnectors_ShouldReturnAllRegistered()
    {
        var names = _orchestrator.GetAvailableConnectors();

        Assert.Contains("csv_connector", names);
        Assert.Contains("excel_connector", names);
        Assert.Contains("pdf_connector", names);
        Assert.Contains("tikr_connector", names);
    }
}
