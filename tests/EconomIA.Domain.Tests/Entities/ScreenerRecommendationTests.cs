using EconomIA.Domain.Entities;
using EconomIA.Domain.Exceptions;

namespace EconomIA.Domain.Tests.Entities;

public class ScreenerRecommendationTests
{
    private static readonly Guid ProfileId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldCreateRecommendation()
    {
        var rec = ScreenerRecommendation.Create(
            ProfileId, "fund", "Amundi MSCI World",
            "Diversificación global, bajo TER", 85, "core",
            isin: "LU1234567890");

        Assert.NotEqual(Guid.Empty, rec.Id);
        Assert.Equal(ProfileId, rec.ProfileId);
        Assert.Equal("fund", rec.EntityType);
        Assert.Equal("Amundi MSCI World", rec.EntityName);
        Assert.Equal(85m, rec.Score);
        Assert.Equal("core", rec.Category);
        Assert.Equal("LU1234567890", rec.Isin);
        Assert.Equal("active", rec.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyEntityName_ShouldThrow(string? name)
    {
        Assert.Throws<DomainException>(() =>
            ScreenerRecommendation.Create(ProfileId, "fund", name!, "reason", 50, "tactical"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(150)]
    public void Create_WithInvalidScore_ShouldThrow(decimal score)
    {
        Assert.Throws<DomainException>(() =>
            ScreenerRecommendation.Create(ProfileId, "fund", "Test", "reason", score, "tactical"));
    }

    [Fact]
    public void Create_WithInvalidCategory_ShouldFallbackToTactical()
    {
        var rec = ScreenerRecommendation.Create(ProfileId, "company", "Apple", "Good company", 70, "invalid_cat", "AAPL");

        Assert.Equal("tactical", rec.Category);
    }

    [Fact]
    public void Dismiss_ShouldChangeStatus()
    {
        var rec = ScreenerRecommendation.Create(ProfileId, "fund", "Test Fund", "reason", 50, "tactical");

        rec.Dismiss();

        Assert.Equal("dismissed", rec.Status);
        Assert.NotNull(rec.ReviewedAt);
    }

    [Fact]
    public void Save_ShouldChangeStatus()
    {
        var rec = ScreenerRecommendation.Create(ProfileId, "company", "Apple", "reason", 80, "core", "AAPL");

        rec.Save();

        Assert.Equal("saved", rec.Status);
        Assert.NotNull(rec.ReviewedAt);
    }

    [Fact]
    public void MarkInvested_ShouldChangeStatus()
    {
        var rec = ScreenerRecommendation.Create(ProfileId, "fund", "iShares", "reason", 90, "core");

        rec.MarkInvested();

        Assert.Equal("invested", rec.Status);
        Assert.NotNull(rec.ReviewedAt);
    }

    [Theory]
    [InlineData("core")]
    [InlineData("tactical")]
    [InlineData("speculative")]
    public void Create_ValidCategories_ShouldAccept(string category)
    {
        var rec = ScreenerRecommendation.Create(ProfileId, "fund", "Test", "reason", 50, category);

        Assert.Equal(category, rec.Category);
    }

    [Fact]
    public void Create_WithMetrics_ShouldStore()
    {
        var metrics = "{\"return1Y\": 15.3, \"ter\": 0.2}";
        var rec = ScreenerRecommendation.Create(ProfileId, "fund", "Test", "reason", 75, "core", metrics: metrics);

        Assert.Equal(metrics, rec.Metrics);
    }
}
