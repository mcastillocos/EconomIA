using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;

namespace EconomIA.Infrastructure.Messaging;

public class NoOpEventBus : IEventBus
{
    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IDomainEvent
        => Task.CompletedTask;
}
