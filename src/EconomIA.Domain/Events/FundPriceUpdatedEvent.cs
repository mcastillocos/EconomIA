using EconomIA.Domain.Entities;

namespace EconomIA.Domain.Events;

public record FundPriceUpdatedEvent(
    Guid FundId,
    string FundName,
    decimal OldPrice,
    decimal NewPrice,
    string Currency) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
