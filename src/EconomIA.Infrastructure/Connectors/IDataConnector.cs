namespace EconomIA.Infrastructure.Connectors;

public interface IDataConnector
{
    string ConnectorName { get; }
    string[] SupportedFileTypes { get; }
    Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default);
}

public record ConnectorMetadata
{
    public string FileName { get; init; } = string.Empty;
    public string? EntityType { get; init; }
    public string? EntityName { get; init; }
    public string? Ticker { get; init; }
    public string? Isin { get; init; }
    public string? Source { get; init; }
}
