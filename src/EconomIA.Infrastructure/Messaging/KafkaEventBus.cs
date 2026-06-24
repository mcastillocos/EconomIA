using System.Text.Json;
using Confluent.Kafka;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EconomIA.Infrastructure.Messaging;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "economia-group";
}

public class KafkaEventBus : IEventBus, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventBus> _logger;

    public KafkaEventBus(IOptions<KafkaOptions> options, ILogger<KafkaEventBus> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IDomainEvent
    {
        var topic = typeof(T).Name.Replace("Event", "").ToLowerInvariant();
        var message = new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = JsonSerializer.Serialize(@event, @event.GetType())
        };

        var result = await _producer.ProduceAsync(topic, message, ct);
        _logger.LogInformation("Published event {EventType} to topic {Topic} at offset {Offset}",
            typeof(T).Name, topic, result.Offset);
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
