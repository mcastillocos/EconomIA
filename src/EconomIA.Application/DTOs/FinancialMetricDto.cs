namespace EconomIA.Application.DTOs;

public record FinancialMetricDto(
    Guid Id,
    string EntityType,
    Guid? EntityId,
    string? Ticker,
    string? Isin,
    string MetricName,
    decimal Value,
    string? Period,
    int? Year,
    int? Quarter,
    string? Currency,
    string? Source,
    string? SourceType,
    string? FileName,
    string? Page,
    string? Row,
    string? Url,
    string Confidence,
    string? RawText,
    bool Validated,
    DateTime? ValidatedAt,
    DateTime CreatedAt);

public record MetricFilterRequest(
    string? EntityType = null,
    Guid? EntityId = null,
    string? MetricName = null,
    int? Year = null,
    int? Quarter = null,
    string? Source = null,
    bool? Validated = null);
