using EconomIA.Application.DTOs;
using EconomIA.Domain.Ports;
using EconomIA.Domain.ValueObjects;
using MediatR;

namespace EconomIA.Application.Queries.GetFundsByRisk;

public class GetFundsByRiskHandler : IRequestHandler<GetFundsByRiskQuery, IReadOnlyList<FundDto>>
{
    private readonly IFundRepository _fundRepository;

    public GetFundsByRiskHandler(IFundRepository fundRepository)
    {
        _fundRepository = fundRepository;
    }

    public async Task<IReadOnlyList<FundDto>> Handle(GetFundsByRiskQuery request, CancellationToken cancellationToken)
    {
        var funds = await _fundRepository.GetByRiskLevelAsync(request.RiskLevel, cancellationToken);

        return funds.Select(f => new FundDto(
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
            null)).ToList();
    }
}
