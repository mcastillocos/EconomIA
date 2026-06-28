using EconomIA.Domain.Entities;

namespace EconomIA.Domain.Ports;

public interface IWatchlistRepository
{
    Task<Watchlist?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Watchlist>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Watchlist watchlist, CancellationToken ct = default);
    Task UpdateAsync(Watchlist watchlist, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
