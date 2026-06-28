namespace EconomIA.Infrastructure.Connectors;

public record NormalizedDataPoint
{
    public string Source { get; init; } = string.Empty;
    public string SourceType { get; init; } = string.Empty; // fund | company | news | pdf | csv | excel | email | transcript | audio | api | manual
    public string EntityType { get; init; } = string.Empty; // fund | company | sector | competitor | portfolio
    public string EntityName { get; init; } = string.Empty;
    public string? Ticker { get; init; }
    public string? Isin { get; init; }
    public string? Market { get; init; }
    public string? Country { get; init; }
    public string? Sector { get; init; }
    public string? Industry { get; init; }
    public string Metric { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public string? Period { get; init; }
    public int? Year { get; init; }
    public int? Quarter { get; init; }
    public string? Currency { get; init; }
    public string? Url { get; init; }
    public string? FileName { get; init; }
    public string? Page { get; init; }
    public string? Row { get; init; }
    public DateTime RetrievedAt { get; init; } = DateTime.UtcNow;
    public string Confidence { get; init; } = "medium"; // high | medium | low
    public string? RawReference { get; init; }
    public string? RawText { get; init; }
}
