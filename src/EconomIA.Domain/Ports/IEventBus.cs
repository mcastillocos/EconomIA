using EconomIA.Domain.Entities;

namespace EconomIA.Domain.Ports;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IDomainEvent;
}
