using Microsoft.Extensions.Logging;

namespace EconomIA.Infrastructure.Connectors;

/// <summary>
/// Orquestador de conectores: enruta documentos/streams al conector apropiado
/// según tipo de archivo o fuente, y agrega resultados normalizados.
/// </summary>
public class ConnectorOrchestrator
{
    private readonly IEnumerable<IDataConnector> _connectors;
    private readonly ILogger<ConnectorOrchestrator> _logger;

    public ConnectorOrchestrator(IEnumerable<IDataConnector> connectors, ILogger<ConnectorOrchestrator> logger)
    {
        _connectors = connectors;
        _logger = logger;
    }

    /// <summary>
    /// Procesa un archivo/stream usando el conector adecuado según extensión.
    /// </summary>
    public async Task<ConnectorResult> ProcessAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(metadata.FileName).TrimStart('.').ToLowerInvariant();
        var connector = ResolveConnector(extension, metadata.Source);

        if (connector is null)
        {
            _logger.LogWarning("No hay conector disponible para extensión '{Extension}' o fuente '{Source}'", extension, metadata.Source);
            return ConnectorResult.NoConnector(metadata.FileName);
        }

        _logger.LogInformation("Procesando '{File}' con conector '{Connector}'", metadata.FileName, connector.ConnectorName);

        try
        {
            var points = await connector.ExtractAsync(stream, metadata, ct);
            _logger.LogInformation("Extraídos {Count} data points de '{File}'", points.Count, metadata.FileName);
            return ConnectorResult.Success(connector.ConnectorName, points);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando '{File}' con conector '{Connector}'", metadata.FileName, connector.ConnectorName);
            return ConnectorResult.Error(connector.ConnectorName, ex.Message);
        }
    }

    /// <summary>
    /// Procesa múltiples archivos en paralelo con un grado de concurrencia controlado.
    /// </summary>
    public async Task<IReadOnlyList<ConnectorResult>> ProcessBatchAsync(
        IReadOnlyList<(Stream Stream, ConnectorMetadata Metadata)> items,
        int maxConcurrency = 4,
        CancellationToken ct = default)
    {
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = items.Select(async item =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                return await ProcessAsync(item.Stream, item.Metadata, ct);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        return results;
    }

    /// <summary>
    /// Procesa contenido de un conector específico por nombre.
    /// </summary>
    public async Task<ConnectorResult> ProcessWithConnectorAsync(string connectorName, Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        var connector = _connectors.FirstOrDefault(c => c.ConnectorName.Equals(connectorName, StringComparison.OrdinalIgnoreCase));
        if (connector is null)
            return ConnectorResult.NoConnector(metadata.FileName);

        try
        {
            var points = await connector.ExtractAsync(stream, metadata, ct);
            return ConnectorResult.Success(connector.ConnectorName, points);
        }
        catch (Exception ex)
        {
            return ConnectorResult.Error(connector.ConnectorName, ex.Message);
        }
    }

    public IReadOnlyList<string> GetAvailableConnectors() =>
        _connectors.Select(c => c.ConnectorName).ToList();

    private IDataConnector? ResolveConnector(string extension, string? source)
    {
        // Prioridad 1: por nombre de fuente explícito
        if (!string.IsNullOrWhiteSpace(source))
        {
            var bySource = _connectors.FirstOrDefault(c =>
                c.ConnectorName.Contains(source, StringComparison.OrdinalIgnoreCase));
            if (bySource is not null)
                return bySource;
        }

        // Prioridad 2: por tipo de archivo
        if (!string.IsNullOrWhiteSpace(extension))
        {
            var byType = _connectors.FirstOrDefault(c =>
                c.SupportedFileTypes.Contains(extension, StringComparer.OrdinalIgnoreCase));
            if (byType is not null)
                return byType;
        }

        return null;
    }
}

public record ConnectorResult
{
    public bool IsSuccess { get; init; }
    public string ConnectorUsed { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<NormalizedDataPoint> DataPoints { get; init; } = [];

    public static ConnectorResult Success(string connector, IReadOnlyList<NormalizedDataPoint> points) =>
        new() { IsSuccess = true, ConnectorUsed = connector, DataPoints = points };

    public static ConnectorResult NoConnector(string fileName) =>
        new() { IsSuccess = false, FileName = fileName, ErrorMessage = "No hay conector disponible" };

    public static ConnectorResult Error(string connector, string message) =>
        new() { IsSuccess = false, ConnectorUsed = connector, ErrorMessage = message };
}
