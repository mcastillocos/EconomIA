using EconomIA.Domain.Entities;

namespace EconomIA.Domain.Events;

public record FundRankingChangedEvent(
    Guid FundId,
    string FundName,
    int OldPosition,
    int NewPosition) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
