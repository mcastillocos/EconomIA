using EconomIA.Application.DTOs;
using EconomIA.Domain.Ports;
using MediatR;

namespace EconomIA.Application.Queries.GetTopFunds;

public class GetTopFundsHandler : IRequestHandler<GetTopFundsQuery, IReadOnlyList<FundDto>>
{
    private readonly IFundRepository _fundRepository;
    private readonly ICacheService _cacheService;

    public GetTopFundsHandler(IFundRepository fundRepository, ICacheService cacheService)
    {
        _fundRepository = fundRepository;
        _cacheService = cacheService;
    }

    public async Task<IReadOnlyList<FundDto>> Handle(GetTopFundsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"top-funds:{request.Count}";
        var cached = await _cacheService.GetAsync<List<FundDto>>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var funds = await _fundRepository.GetTopFundsAsync(request.Count, cancellationToken);

        var dtos = funds.Select(f =>
        {
            var latestPerf = f.Performances.MaxBy(p => p.RecordedAt);
            FundPerformanceDto? perfDto = latestPerf is null ? null : new FundPerformanceDto(
                latestPerf.Return1Month.Value,
                latestPerf.Return3Months.Value,
                latestPerf.Return6Months.Value,
                latestPerf.Return1Year.Value,
                latestPerf.Return3Years.Value,
                latestPerf.Return5Years.Value,
                latestPerf.Volatility.Value,
                latestPerf.SharpeRatio,
                latestPerf.RecordedAt);

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
                perfDto);
        }).ToList();

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5), cancellationToken);
        return dtos;
    }
}
