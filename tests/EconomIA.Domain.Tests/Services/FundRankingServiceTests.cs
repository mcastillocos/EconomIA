using EconomIA.Domain.Entities;
using EconomIA.Domain.Services;
using EconomIA.Domain.ValueObjects;
using FluentAssertions;

namespace EconomIA.Domain.Tests.Services;

public class FundRankingServiceTests
{
    private readonly FundRankingService _service = new();

    [Fact]
    public void CalculateRanking_ShouldOrderByScore_HighestFirst()
    {
        // Arrange
        var fund1 = CreateFundWithPerformance("ES0000000001", "Low Performer", return1Y: 5m, sharpe: 0.5m);
        var fund2 = CreateFundWithPerformance("ES0000000002", "High Performer", return1Y: 25m, sharpe: 2.5m);
        var fund3 = CreateFundWithPerformance("ES0000000003", "Mid Performer", return1Y: 15m, sharpe: 1.5m);

        // Act
        var ranked = _service.CalculateRanking([fund1, fund2, fund3]);

        // Assert
        ranked.Should().HaveCount(3);
        ranked[0].Name.Should().Be("High Performer");
        ranked[0].RankingPosition.Should().Be(1);
        ranked[1].Name.Should().Be("Mid Performer");
        ranked[1].RankingPosition.Should().Be(2);
        ranked[2].Name.Should().Be("Low Performer");
        ranked[2].RankingPosition.Should().Be(3);
    }

    [Fact]
    public void CalculateRanking_HighSharpe_ShouldGetFiveStars()
    {
        // Arrange
        var fund = CreateFundWithPerformance("ES0000000001", "Star Fund", return1Y: 20m, sharpe: 2.5m);

        // Act
        var ranked = _service.CalculateRanking([fund]);

        // Assert
        ranked[0].Rating.Should().Be(FundRating.FiveStars);
    }

    [Fact]
    public void CalculateRanking_LowSharpe_ShouldGetOneStar()
    {
        // Arrange
        var fund = CreateFundWithPerformance("ES0000000001", "Weak Fund", return1Y: 2m, sharpe: 0.3m);

        // Act
        var ranked = _service.CalculateRanking([fund]);

        // Assert
        ranked[0].Rating.Should().Be(FundRating.OneStar);
    }

    [Fact]
    public void CalculateRanking_NoPerformance_ShouldRankLast()
    {
        // Arrange
        var fundWithPerf = CreateFundWithPerformance("ES0000000001", "Has Data", return1Y: 10m, sharpe: 1.2m);
        var fundWithout = Fund.Create(
            new ISIN("ES0000000002"), "No Data", "Category", "Company",
            RiskLevel.Medium, new Money(100m, "EUR"), new Percentage(1.0m));
        fundWithout.ClearDomainEvents();

        // Act
        var ranked = _service.CalculateRanking([fundWithout, fundWithPerf]);

        // Assert
        ranked[0].Name.Should().Be("Has Data");
        ranked[1].Name.Should().Be("No Data");
        ranked[1].Rating.Should().Be(FundRating.Unrated);
    }

    [Fact]
    public void CalculateRanking_HigherExpenseRatio_ShouldPenalizeRanking()
    {
        // Two funds with same returns but different expense ratios
        var cheapFund = CreateFundWithPerformance("ES0000000001", "Cheap Fund", return1Y: 15m, sharpe: 1.5m, expenseRatio: 0.2m);
        var expensiveFund = CreateFundWithPerformance("ES0000000002", "Expensive Fund", return1Y: 15m, sharpe: 1.5m, expenseRatio: 2.5m);

        // Act
        var ranked = _service.CalculateRanking([expensiveFund, cheapFund]);

        // Assert
        ranked[0].Name.Should().Be("Cheap Fund");
        ranked[1].Name.Should().Be("Expensive Fund");
    }

    private static Fund CreateFundWithPerformance(string isin, string name, decimal return1Y, decimal sharpe, decimal expenseRatio = 1.0m)
    {
        var fund = Fund.Create(
            new ISIN(isin), name, "Category", "Company",
            RiskLevel.Medium, new Money(100m, "EUR"), new Percentage(expenseRatio));
        fund.ClearDomainEvents();

        var perf = FundPerformance.Create(
            fund.Id,
            new Percentage(return1Y * 0.1m),
            new Percentage(return1Y * 0.3m),
            new Percentage(return1Y * 0.5m),
            new Percentage(return1Y),
            new Percentage(return1Y * 2.5m),
            new Percentage(return1Y * 4m),
            new Percentage(12m),
            sharpe);

        fund.AddPerformance(perf);
        return fund;
    }
}
