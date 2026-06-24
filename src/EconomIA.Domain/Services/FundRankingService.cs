using EconomIA.Domain.Entities;
using EconomIA.Domain.ValueObjects;

namespace EconomIA.Domain.Services;

public class FundRankingService
{
    public IReadOnlyList<Fund> CalculateRanking(IEnumerable<Fund> funds)
    {
        return funds
            .OrderByDescending(f => CalculateScore(f))
            .Select((fund, index) =>
            {
                fund.UpdateRanking(index + 1, CalculateRating(fund));
                return fund;
            })
            .ToList();
    }

    private static decimal CalculateScore(Fund fund)
    {
        var latestPerformance = fund.Performances.MaxBy(p => p.RecordedAt);
        if (latestPerformance is null) return 0;

        var returnScore = latestPerformance.Return1Year.Value * 0.3m
                        + latestPerformance.Return3Years.Value * 0.25m
                        + latestPerformance.Return5Years.Value * 0.2m;

        var riskPenalty = (int)fund.RiskLevel * 0.05m;
        var expensePenalty = fund.ExpenseRatio.Value * 0.1m;
        var sharpeBonus = latestPerformance.SharpeRatio * 0.15m;

        return returnScore - riskPenalty - expensePenalty + sharpeBonus;
    }

    private static FundRating CalculateRating(Fund fund)
    {
        var latestPerformance = fund.Performances.MaxBy(p => p.RecordedAt);
        if (latestPerformance is null) return FundRating.Unrated;

        var sharpe = latestPerformance.SharpeRatio;

        return sharpe switch
        {
            > 2.0m => FundRating.FiveStars,
            > 1.5m => FundRating.FourStars,
            > 1.0m => FundRating.ThreeStars,
            > 0.5m => FundRating.TwoStars,
            _ => FundRating.OneStar
        };
    }
}
