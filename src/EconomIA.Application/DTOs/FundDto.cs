using EconomIA.Domain.ValueObjects;

namespace EconomIA.Application.DTOs;

public record FundDto(
    Guid Id,
    string Isin,
    string Name,
    string Category,
    string ManagementCompany,
    RiskLevel RiskLevel,
    decimal NetAssetValue,
    string Currency,
    decimal ExpenseRatio,
    FundRating Rating,
    int RankingPosition,
    DateTime LastUpdated,
    FundPerformanceDto? LatestPerformance);

public record FundPerformanceDto(
    decimal Return1Month,
    decimal Return3Months,
    decimal Return6Months,
    decimal Return1Year,
    decimal Return3Years,
    decimal Return5Years,
    decimal Volatility,
    decimal SharpeRatio,
    DateTime RecordedAt);
