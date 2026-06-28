using EconomIA.Application.Commands.RefreshMarketData;
using EconomIA.Infrastructure.Telemetry;
using MediatR;

namespace EconomIA.API.BackgroundServices;

public class MarketDataPollingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketDataPollingService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(5);

    public MarketDataPollingService(IServiceProvider serviceProvider, ILogger<MarketDataPollingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Market data polling service started. Interval: {Interval}", _pollingInterval);

        // Initial delay to let the app start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                _logger.LogInformation("Refreshing market data...");
                var count = await mediator.Send(new RefreshMarketDataCommand(100), stoppingToken);
                OpenTelemetryConfig.FundsUpdated.Add(count);
                _logger.LogInformation("Market data refreshed. {Count} funds updated", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing market data");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }
}
