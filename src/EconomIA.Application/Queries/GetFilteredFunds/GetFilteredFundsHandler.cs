using EconomIA.Application.DTOs;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using MediatR;

namespace EconomIA.Application.Queries.GetFilteredFunds;

public class GetFilteredFundsHandler : IRequestHandler<GetFilteredFundsQuery, FilteredFundsResponse>
{
    private readonly IFundRepository _fundRepository;

    public GetFilteredFundsHandler(IFundRepository fundRepository)
    {
        _fundRepository = fundRepository;
    }

    public async Task<FilteredFundsResponse> Handle(GetFilteredFundsQuery request, CancellationToken cancellationToken)
    {
        var (funds, totalCount) = await _fundRepository.GetFilteredAsync(
            riskLevel: request.RiskLevel,
            category: request.Category,
            managementCompany: request.ManagementCompany,
            minRating: request.MinRating,
            maxExpenseRatio: request.MaxExpenseRatio,
            minReturn1Year: request.MinReturn1Year,
            maxVolatility: request.MaxVolatility,
            searchTerm: request.SearchTerm,
            sortBy: request.SortBy.ToString(),
            sortDescending: request.SortDescending,
            page: request.Page,
            pageSize: request.PageSize,
            ct: cancellationToken);

        var items = funds.Select(MapToDto).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new FilteredFundsResponse(items, totalCount, request.Page, request.PageSize, totalPages);
    }

    private static FundDto MapToDto(Fund f)
    {
        var latestPerf = f.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();

        return new FundDto(
            f.Id,
            f.Isin.Value,
            f.Name,
            f.Category,
            f.ManagementCompany,
            f.RiskLevel,
            f.NetAssetValue.Amount,
            f.NetAssetValue.Currency,
            f.ExpenseRatio.Value,
            f.Rating,
            f.RankingPosition,
            f.LastUpdated,
            latestPerf is null ? null : new FundPerformanceDto(
                latestPerf.Return1Month.Value,
                latestPerf.Return3Months.Value,
                latestPerf.Return6Months.Value,
                latestPerf.Return1Year.Value,
                latestPerf.Return3Years.Value,
                latestPerf.Return5Years.Value,
                latestPerf.Volatility.Value,
                latestPerf.SharpeRatio,
                latestPerf.RecordedAt));
    }
}
