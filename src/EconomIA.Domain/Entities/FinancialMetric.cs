namespace EconomIA.Domain.Entities;

public class FinancialMetric : Entity<Guid>
{
    public string EntityType { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public string? Ticker { get; private set; }
    public string? Isin { get; private set; }
    public string MetricName { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public string? Period { get; private set; }
    public int? Year { get; private set; }
    public int? Quarter { get; private set; }
    public string? Currency { get; private set; }
    public string? Source { get; private set; }
    public string? SourceType { get; private set; }
    public string? FileName { get; private set; }
    public string? Page { get; private set; }
    public string? Row { get; private set; }
    public string? Url { get; private set; }
    public string Confidence { get; private set; } = "medium";
    public string? RawText { get; private set; }
    public bool Validated { get; private set; }
    public DateTime? ValidatedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private FinancialMetric() { }

    public static FinancialMetric Create(
        string entityType,
        Guid? entityId,
        string metricName,
        decimal value,
        string? ticker = null,
        string? isin = null,
        string? period = null,
        int? year = null,
        int? quarter = null,
        string? currency = null,
        string? source = null,
        string? sourceType = null,
        string? fileName = null,
        string? page = null,
        string? row = null,
        string? url = null,
        string confidence = "medium",
        string? rawText = null)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new Exceptions.DomainException("Metric name cannot be empty.");

        return new FinancialMetric
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Ticker = ticker,
            Isin = isin,
            MetricName = metricName,
            Value = value,
            Period = period,
            Year = year,
            Quarter = quarter,
            Currency = currency,
            Source = source,
            SourceType = sourceType,
            FileName = fileName,
            Page = page,
            Row = row,
            Url = url,
            Confidence = confidence,
            RawText = rawText,
            Validated = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkValidated()
    {
        Validated = true;
        ValidatedAt = DateTime.UtcNow;
    }

    public void MarkUnvalidated()
    {
        Validated = false;
        ValidatedAt = null;
    }
}
