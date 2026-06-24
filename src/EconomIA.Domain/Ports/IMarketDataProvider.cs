using EconomIA.Domain.Entities;

namespace EconomIA.Domain.Ports;

public interface IMarketDataProvider
{
    Task<IReadOnlyList<Fund>> FetchTopFundsAsync(int count, CancellationToken ct = default);
    Task<Fund?> FetchFundByIsinAsync(string isin, CancellationToken ct = default);
    Task<FundPerformance?> FetchPerformanceAsync(Guid fundId, string isin, CancellationToken ct = default);
}
