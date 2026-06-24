using EconomIA.Domain.Ports;
using EconomIA.Domain.Services;
using EconomIA.Application.Interfaces;
using MediatR;

namespace EconomIA.Application.Commands.RefreshMarketData;

public class RefreshMarketDataHandler : IRequestHandler<RefreshMarketDataCommand, int>
{
    private readonly IMarketDataProvider _marketDataProvider;
    private readonly IFundRepository _fundRepository;
    private readonly ICacheService _cacheService;
    private readonly IEventBus _eventBus;
    private readonly IFundNotificationService _notificationService;
    private readonly FundRankingService _rankingService;

    public RefreshMarketDataHandler(
        IMarketDataProvider marketDataProvider,
        IFundRepository fundRepository,
        ICacheService cacheService,
        IEventBus eventBus,
        IFundNotificationService notificationService,
        FundRankingService rankingService)
    {
        _marketDataProvider = marketDataProvider;
        _fundRepository = fundRepository;
        _cacheService = cacheService;
        _eventBus = eventBus;
        _notificationService = notificationService;
        _rankingService = rankingService;
    }

    public async Task<int> Handle(RefreshMarketDataCommand request, CancellationToken cancellationToken)
    {
        var funds = await _marketDataProvider.FetchTopFundsAsync(request.TopCount, cancellationToken);

        foreach (var fund in funds)
        {
            var existing = await _fundRepository.GetByIsinAsync(fund.Isin, cancellationToken);
            if (existing is null)
            {
                await _fundRepository.AddAsync(fund, cancellationToken);
            }
            else
            {
                existing.UpdatePrice(fund.NetAssetValue);
                await _fundRepository.UpdateAsync(existing, cancellationToken);
            }
        }

        await _fundRepository.SaveChangesAsync(cancellationToken);

        // Recalculate ranking
        var allFunds = await _fundRepository.GetTopFundsAsync(request.TopCount, cancellationToken);
        _rankingService.CalculateRanking(allFunds);
        await _fundRepository.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveAsync("top-funds:100", cancellationToken);

        // Notify clients
        await _notificationService.NotifyTopFundsUpdatedAsync(cancellationToken);

        return funds.Count;
    }
}
