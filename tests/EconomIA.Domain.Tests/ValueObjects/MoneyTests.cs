using EconomIA.Domain.ValueObjects;
using EconomIA.Domain.Exceptions;
using FluentAssertions;

namespace EconomIA.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateMoney()
    {
        var money = new Money(100.50m, "EUR");
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Create_ShouldNormalizeCurrency()
    {
        var money = new Money(50m, "eur");
        money.Currency.Should().Be("EUR");
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    public void Create_WithInvalidCurrency_ShouldThrow(string currency)
    {
        var act = () => new Money(100m, currency);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Add_SameCurrency_ShouldReturnSum()
    {
        var m1 = new Money(100m, "EUR");
        var m2 = new Money(50m, "EUR");

        var result = m1.Add(m2);

        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Add_DifferentCurrency_ShouldThrow()
    {
        var m1 = new Money(100m, "EUR");
        var m2 = new Money(50m, "USD");

        var act = () => m1.Add(m2);
        act.Should().Throw<DomainException>().WithMessage("*different currencies*");
    }

    [Fact]
    public void TwoMoneyWithSameValues_ShouldBeEqual()
    {
        var m1 = new Money(100m, "EUR");
        var m2 = new Money(100m, "EUR");
        m1.Should().Be(m2);
    }
}
