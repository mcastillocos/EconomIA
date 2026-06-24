using EconomIA.Domain.Entities;
using EconomIA.Domain.ValueObjects;
using EconomIA.Domain.Exceptions;
using FluentAssertions;

namespace EconomIA.Domain.Tests.Entities;

public class FundTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateFund()
    {
        // Arrange
        var isin = new ISIN("ES0000000001");
        var name = "Test Fund";
        var category = "Renta Variable";
        var company = "BlackRock";
        var risk = RiskLevel.Medium;
        var nav = new Money(100m, "EUR");
        var expense = new Percentage(1.5m);

        // Act
        var fund = Fund.Create(isin, name, category, company, risk, nav, expense);

        // Assert
        fund.Should().NotBeNull();
        fund.Id.Should().NotBeEmpty();
        fund.Isin.Value.Should().Be("ES0000000001");
        fund.Name.Should().Be(name);
        fund.Category.Should().Be(category);
        fund.RiskLevel.Should().Be(RiskLevel.Medium);
        fund.NetAssetValue.Amount.Should().Be(100m);
        fund.Rating.Should().Be(FundRating.Unrated);
        fund.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowDomainException()
    {
        // Arrange & Act
        var act = () => Fund.Create(
            new ISIN("ES0000000001"),
            "",
            "Category",
            "Company",
            RiskLevel.Medium,
            new Money(100m, "EUR"),
            new Percentage(1.5m));

        // Assert
        act.Should().Throw<DomainException>().WithMessage("*name*");
    }

    [Fact]
    public void UpdatePrice_ShouldUpdateNavAndRaiseEvent()
    {
        // Arrange
        var fund = CreateTestFund();
        var newPrice = new Money(150m, "EUR");

        // Act
        fund.UpdatePrice(newPrice);

        // Assert
        fund.NetAssetValue.Amount.Should().Be(150m);
        fund.DomainEvents.Should().HaveCount(2); // Create + PriceUpdate
    }

    [Fact]
    public void UpdateRanking_ShouldChangePositionAndRaiseEvent()
    {
        // Arrange
        var fund = CreateTestFund();
        fund.ClearDomainEvents();

        // Act
        fund.UpdateRanking(5, FundRating.FourStars);

        // Assert
        fund.RankingPosition.Should().Be(5);
        fund.Rating.Should().Be(FundRating.FourStars);
        fund.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void UpdateRanking_SamePosition_ShouldNotRaiseEvent()
    {
        // Arrange
        var fund = CreateTestFund();
        fund.UpdateRanking(5, FundRating.ThreeStars);
        fund.ClearDomainEvents();

        // Act
        fund.UpdateRanking(5, FundRating.FourStars);

        // Assert
        fund.DomainEvents.Should().BeEmpty();
    }

    private static Fund CreateTestFund()
    {
        return Fund.Create(
            new ISIN("ES0000000001"),
            "Test Fund",
            "Renta Variable",
            "BlackRock",
            RiskLevel.Medium,
            new Money(100m, "EUR"),
            new Percentage(1.5m));
    }
}
