using EconomIA.Domain.ValueObjects;
using EconomIA.Domain.Exceptions;
using FluentAssertions;

namespace EconomIA.Domain.Tests.ValueObjects;

public class ISINTests
{
    [Theory]
    [InlineData("ES0000000001")]
    [InlineData("LU1234567890")]
    [InlineData("IE00B4L5Y983")]
    [InlineData("US0378331005")]
    public void Create_WithValidISIN_ShouldSucceed(string value)
    {
        var isin = new ISIN(value);
        isin.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyValue_ShouldThrow(string? value)
    {
        var act = () => new ISIN(value!);
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("ES123")] // Too short
    [InlineData("1234567890AB")] // Starts with numbers
    [InlineData("ES00000000001")] // Too long
    public void Create_WithInvalidFormat_ShouldThrow(string value)
    {
        var act = () => new ISIN(value);
        act.Should().Throw<DomainException>().WithMessage("*Invalid ISIN*");
    }

    [Fact]
    public void Create_ShouldNormalizeToUpperCase()
    {
        var isin = new ISIN("es0000000001");
        isin.Value.Should().Be("ES0000000001");
    }

    [Fact]
    public void TwoISINsWithSameValue_ShouldBeEqual()
    {
        var isin1 = new ISIN("ES0000000001");
        var isin2 = new ISIN("ES0000000001");
        isin1.Should().Be(isin2);
    }
}
