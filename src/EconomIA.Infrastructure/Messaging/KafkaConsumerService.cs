using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EconomIA.Infrastructure.Messaging;

public class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly KafkaOptions _options;
    private IConsumer<string, string>? _consumer;

    public event Func<string, string, Task>? OnMessageReceived;

    public KafkaConsumerService(IOptions<KafkaOptions> options, ILogger<KafkaConsumerService> logger)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(["fundpriceupdated", "fundrankingchanged", "newfunddiscovered"]);

        _logger.LogInformation("Kafka consumer started, listening to fund topics");

        await Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);
                    if (result?.Message?.Value is not null)
                    {
                        _logger.LogDebug("Received message from topic {Topic}", result.Topic);
                        if (OnMessageReceived is not null)
                        {
                            await OnMessageReceived(result.Topic, result.Message.Value);
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
                    await Task.Delay(5000, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Kafka connection error, retrying in 10s...");
                    await Task.Delay(10000, stoppingToken);
                }
            }
        }, stoppingToken);
    }

    public override void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        base.Dispose();
    }
}
