namespace EconomIA.Application.DTOs;

public record CompanyDto(
    Guid Id,
    string Name,
    string? Ticker,
    string? Isin,
    string? Market,
    string? Country,
    string? Sector,
    string? Industry,
    string? Currency,
    string? Competitors,
    string? RelevantUrls,
    string? PreferredSource,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateCompanyRequest(
    string Name,
    string? Ticker,
    string? Isin,
    string? Market,
    string? Country,
    string? Sector,
    string? Industry,
    string? Currency,
    string? Competitors,
    string? RelevantUrls,
    string? PreferredSource,
    string? Notes);

public record UpdateCompanyRequest(
    string Name,
    string? Ticker,
    string? Isin,
    string? Market,
    string? Country,
    string? Sector,
    string? Industry,
    string? Currency,
    string? Competitors,
    string? RelevantUrls,
    string? PreferredSource,
    string? Notes);
