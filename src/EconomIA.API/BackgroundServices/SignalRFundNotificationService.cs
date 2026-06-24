using EconomIA.Application.Interfaces;
using EconomIA.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EconomIA.API.BackgroundServices;

public class SignalRFundNotificationService : IFundNotificationService
{
    private readonly IHubContext<FundPriceHub> _priceHub;
    private readonly IHubContext<RankingHub> _rankingHub;

    public SignalRFundNotificationService(
        IHubContext<FundPriceHub> priceHub,
        IHubContext<RankingHub> rankingHub)
    {
        _priceHub = priceHub;
        _rankingHub = rankingHub;
    }

    public async Task NotifyPriceUpdateAsync(Guid fundId, string fundName, decimal newPrice, string currency, CancellationToken ct = default)
    {
        var payload = new { FundId = fundId, FundName = fundName, Price = newPrice, Currency = currency, Timestamp = DateTime.UtcNow };

        await _priceHub.Clients.Group("fund-watchers").SendAsync("PriceUpdated", payload, ct);
        await _priceHub.Clients.Group($"fund-{fundId}").SendAsync("FundPriceUpdated", payload, ct);
    }

    public async Task NotifyRankingChangeAsync(Guid fundId, string fundName, int oldPosition, int newPosition, CancellationToken ct = default)
    {
        var payload = new { FundId = fundId, FundName = fundName, OldPosition = oldPosition, NewPosition = newPosition, Timestamp = DateTime.UtcNow };
        await _rankingHub.Clients.Group("ranking-watchers").SendAsync("RankingChanged", payload, ct);
    }

    public async Task NotifyTopFundsUpdatedAsync(CancellationToken ct = default)
    {
        await _rankingHub.Clients.Group("ranking-watchers").SendAsync("TopFundsRefreshed", new { Timestamp = DateTime.UtcNow }, ct);
    }
}
