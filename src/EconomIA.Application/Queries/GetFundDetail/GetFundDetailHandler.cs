using EconomIA.Application.DTOs;
using EconomIA.Domain.Ports;
using MediatR;

namespace EconomIA.Application.Queries.GetFundDetail;

public class GetFundDetailHandler : IRequestHandler<GetFundDetailQuery, FundDto?>
{
    private readonly IFundRepository _fundRepository;
    private readonly ICacheService _cacheService;

    public GetFundDetailHandler(IFundRepository fundRepository, ICacheService cacheService)
    {
        _fundRepository = fundRepository;
        _cacheService = cacheService;
    }

    public async Task<FundDto?> Handle(GetFundDetailQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"fund:{request.FundId}";
        var cached = await _cacheService.GetAsync<FundDto>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var fund = await _fundRepository.GetByIdAsync(request.FundId, cancellationToken);
        if (fund is null) return null;

        var latestPerf = fund.Performances.MaxBy(p => p.RecordedAt);
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

        var dto = new FundDto(
            fund.Id,
            fund.Isin.Value,
            fund.Name,
            fund.Category,
            fund.ManagementCompany,
            fund.RiskLevel,
            fund.NetAssetValue.Amount,
            fund.NetAssetValue.Currency,
            fund.ExpenseRatio.Value,
            fund.Rating,
            fund.RankingPosition,
            fund.LastUpdated,
            perfDto);

        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10), cancellationToken);
        return dto;
    }
}
