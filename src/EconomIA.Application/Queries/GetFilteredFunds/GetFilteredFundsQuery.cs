using EconomIA.Application.DTOs;
using EconomIA.Domain.ValueObjects;
using MediatR;

namespace EconomIA.Application.Queries.GetFilteredFunds;

public record GetFilteredFundsQuery : IRequest<FilteredFundsResponse>
{
    public RiskLevel? RiskLevel { get; init; }
    public string? Category { get; init; }
    public string? ManagementCompany { get; init; }
    public FundRating? MinRating { get; init; }
    public decimal? MaxExpenseRatio { get; init; }
    public decimal? MinReturn1Year { get; init; }
    public decimal? MaxVolatility { get; init; }
    public string? SearchTerm { get; init; }
    public FundSortBy SortBy { get; init; } = FundSortBy.Ranking;
    public bool SortDescending { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public enum FundSortBy
{
    Ranking,
    Name,
    Return1Year,
    ExpenseRatio,
    Rating,
    Volatility,
    NetAssetValue
}

public record FilteredFundsResponse(
    IReadOnlyList<FundDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
