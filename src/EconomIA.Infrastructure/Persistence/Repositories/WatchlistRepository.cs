using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Infrastructure.Persistence.Repositories;

public class WatchlistRepository : IWatchlistRepository
{
    private readonly EconomIADbContext _context;

    public WatchlistRepository(EconomIADbContext context)
    {
        _context = context;
    }

    public async Task<Watchlist?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Watchlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public async Task<IReadOnlyList<Watchlist>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Watchlists
            .Include(w => w.Items)
            .OrderBy(w => w.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Watchlist watchlist, CancellationToken ct = default)
    {
        await _context.Watchlists.AddAsync(watchlist, ct);
    }

    public Task UpdateAsync(Watchlist watchlist, CancellationToken ct = default)
    {
        _context.Watchlists.Update(watchlist);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var watchlist = await _context.Watchlists.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (watchlist is not null)
            _context.Watchlists.Remove(watchlist);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
