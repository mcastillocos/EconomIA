using EconomIA.Domain.ValueObjects;

namespace EconomIA.Domain.Entities;

public class Fund : Entity<Guid>
{
    public ISIN Isin { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string ManagementCompany { get; private set; } = string.Empty;
    public RiskLevel RiskLevel { get; private set; }
    public Money NetAssetValue { get; private set; } = null!;
    public Percentage ExpenseRatio { get; private set; } = null!;
    public FundRating Rating { get; private set; }
    public int RankingPosition { get; private set; }
    public DateTime LastUpdated { get; private set; }

    private readonly List<FundPerformance> _performances = [];
    public IReadOnlyList<FundPerformance> Performances => _performances.AsReadOnly();

    private Fund() { } // EF Core

    public static Fund Create(
        ISIN isin,
        string name,
        string category,
        string managementCompany,
        RiskLevel riskLevel,
        Money netAssetValue,
        Percentage expenseRatio)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Exceptions.DomainException("Fund name cannot be empty.");

        var fund = new Fund
        {
            Id = Guid.NewGuid(),
            Isin = isin,
            Name = name,
            Category = category,
            ManagementCompany = managementCompany,
            RiskLevel = riskLevel,
            NetAssetValue = netAssetValue,
            ExpenseRatio = expenseRatio,
            Rating = FundRating.Unrated,
            RankingPosition = 0,
            LastUpdated = DateTime.UtcNow
        };

        fund.RaiseDomainEvent(new Events.NewFundDiscoveredEvent(fund.Id, fund.Name, fund.Isin.Value));
        return fund;
    }

    public void UpdatePrice(Money newPrice)
    {
        var oldPrice = NetAssetValue;
        NetAssetValue = newPrice;
        LastUpdated = DateTime.UtcNow;

        RaiseDomainEvent(new Events.FundPriceUpdatedEvent(Id, Name, oldPrice.Amount, newPrice.Amount, newPrice.Currency));
    }

    public void UpdateRanking(int newPosition, FundRating newRating)
    {
        var oldPosition = RankingPosition;
        RankingPosition = newPosition;
        Rating = newRating;
        LastUpdated = DateTime.UtcNow;

        if (oldPosition != newPosition)
        {
            RaiseDomainEvent(new Events.FundRankingChangedEvent(Id, Name, oldPosition, newPosition));
        }
    }

    public void AddPerformance(FundPerformance performance)
    {
        _performances.Add(performance);
    }
}
