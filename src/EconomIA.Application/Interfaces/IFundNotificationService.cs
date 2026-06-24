namespace EconomIA.Application.Interfaces;

public interface IFundNotificationService
{
    Task NotifyPriceUpdateAsync(Guid fundId, string fundName, decimal newPrice, string currency, CancellationToken ct = default);
    Task NotifyRankingChangeAsync(Guid fundId, string fundName, int oldPosition, int newPosition, CancellationToken ct = default);
    Task NotifyTopFundsUpdatedAsync(CancellationToken ct = default);
}
