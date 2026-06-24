using EconomIA.Domain.Entities;

namespace EconomIA.Domain.Events;

public record NewFundDiscoveredEvent(
    Guid FundId,
    string FundName,
    string Isin) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
