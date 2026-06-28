namespace EconomIA.Infrastructure.Connectors;

/// <summary>Stub: Tikr.com connector — pendiente de implementación con API real.</summary>
public class TikrConnector : IDataConnector
{
    public string ConnectorName => "tikr_connector";
    public string[] SupportedFileTypes => [];

    public Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<NormalizedDataPoint>>(Array.Empty<NormalizedDataPoint>());
    }
}

/// <summary>Stub: Investing.com connector — pendiente de implementación con API real.</summary>
public class InvestingConnector : IDataConnector
{
    public string ConnectorName => "investing_connector";
    public string[] SupportedFileTypes => [];

    public Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<NormalizedDataPoint>>(Array.Empty<NormalizedDataPoint>());
    }
}

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

/// <summary>Stub: News connector — pendiente.</summary>
public class NewsConnector : IDataConnector
{
    public string ConnectorName => "news_connector";
    public string[] SupportedFileTypes => [];

    public Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<NormalizedDataPoint>>(Array.Empty<NormalizedDataPoint>());
    }
}

/// <summary>Stub: Transcript connector — pendiente.</summary>
public class TranscriptConnector : IDataConnector
{
    public string ConnectorName => "transcript_connector";
    public string[] SupportedFileTypes => ["txt"];

    public Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<NormalizedDataPoint>>(Array.Empty<NormalizedDataPoint>());
    }
}

/// <summary>Stub: Audio connector — pendiente.</summary>
public class AudioConnector : IDataConnector
{
    public string ConnectorName => "audio_connector";
    public string[] SupportedFileTypes => ["mp3", "wav", "m4a"];

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
