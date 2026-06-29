using EconomIA.Domain.Entities;
using EconomIA.Domain.Exceptions;

namespace EconomIA.Domain.Tests.Entities;

public class InvestorProfileTests
{
    [Fact]
    public void Create_ShouldSetDefaults()
    {
        var profile = InvestorProfile.Create("user1");

        Assert.NotEqual(Guid.Empty, profile.Id);
        Assert.Equal("user1", profile.UserId);
        Assert.Equal("moderate", profile.RiskTolerance);
        Assert.Equal("medium", profile.InvestmentHorizon);
        Assert.Equal("blend", profile.InvestmentStyle);
        Assert.Equal("both", profile.AssetPreference);
        Assert.Equal(0, profile.InteractionsCount);
    }

    [Fact]
    public void UpdateFromQuestionnaire_ShouldSetAllFields()
    {
        var profile = InvestorProfile.Create();

        profile.UpdateFromQuestionnaire(
            riskTolerance: "aggressive",
            investmentHorizon: "long",
            investmentStyle: "growth",
            assetPreference: "stocks",
            maxExpenseRatio: 0.5m,
            minReturn1Y: 10m,
            esgPreference: true,
            preferredSectors: "[\"Technology\"]",
            preferredGeographies: "[\"USA\"]",
            excludedSectors: "[\"Energy\"]"
        );

        Assert.Equal("aggressive", profile.RiskTolerance);
        Assert.Equal("long", profile.InvestmentHorizon);
        Assert.Equal("growth", profile.InvestmentStyle);
        Assert.Equal("stocks", profile.AssetPreference);
        Assert.Equal(0.5m, profile.MaxExpenseRatio);
        Assert.Equal(10m, profile.MinReturn1Y);
        Assert.True(profile.EsgPreference);
        Assert.Equal("[\"Technology\"]", profile.PreferredSectors);
        Assert.Equal("[\"USA\"]", profile.PreferredGeographies);
        Assert.Equal("[\"Energy\"]", profile.ExcludedSectors);
    }

    [Fact]
    public void UpdateFromQuestionnaire_InvalidEnum_ShouldFallbackToDefault()
    {
        var profile = InvestorProfile.Create();

        profile.UpdateFromQuestionnaire("invalid_risk", "invalid_horizon", "invalid_style", "invalid_asset", 1m, 0m, false, "", "", "");

        Assert.Equal("moderate", profile.RiskTolerance);
        Assert.Equal("medium", profile.InvestmentHorizon);
        Assert.Equal("blend", profile.InvestmentStyle);
        Assert.Equal("both", profile.AssetPreference);
    }

    [Fact]
    public void UpdateFromQuestionnaire_ClampsExpenseRatio()
    {
        var profile = InvestorProfile.Create();

        profile.UpdateFromQuestionnaire("moderate", "medium", "blend", "both", 50m, 0m, false, "", "", "");

        Assert.Equal(10m, profile.MaxExpenseRatio); // Clamped to max 10
    }

    [Fact]
    public void RecordInteraction_ShouldIncrement()
    {
        var profile = InvestorProfile.Create();

        profile.RecordInteraction();
        profile.RecordInteraction();
        profile.RecordInteraction();

        Assert.Equal(3, profile.InteractionsCount);
    }

    [Fact]
    public void SetNotes_ShouldUpdate()
    {
        var profile = InvestorProfile.Create();

        profile.SetNotes("Prefiero empresas con moat");

        Assert.Equal("Prefiero empresas con moat", profile.Notes);
    }
}
