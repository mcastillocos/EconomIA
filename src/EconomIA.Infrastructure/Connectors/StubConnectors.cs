namespace EconomIA.Infrastructure.Connectors;

/// <summary>Stub: Email connector — pendiente.</summary>
public class EmailConnector : IDataConnector
{
    public string ConnectorName => "email_connector";
    public string[] SupportedFileTypes => ["eml", "msg"];

    public Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<NormalizedDataPoint>>(Array.Empty<NormalizedDataPoint>());
    }
}

/// <summary>Stub: Generic API connector — pendiente.</summary>
public class ApiConnector : IDataConnector
{
    public string ConnectorName => "api_connector";
    public string[] SupportedFileTypes => [];

    public Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<NormalizedDataPoint>>(Array.Empty<NormalizedDataPoint>());
    }
}

/// <summary>Stub: Manual data entry connector — pendiente.</summary>
public class ManualConnector : IDataConnector
{
    public string ConnectorName => "manual_connector";
    public string[] SupportedFileTypes => [];

    public Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<NormalizedDataPoint>>(Array.Empty<NormalizedDataPoint>());
    }
}
