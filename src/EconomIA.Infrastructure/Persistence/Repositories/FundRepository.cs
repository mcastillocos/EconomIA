using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using EconomIA.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Infrastructure.Persistence.Repositories;

public class FundRepository : IFundRepository
{
    private readonly EconomIADbContext _context;

    public FundRepository(EconomIADbContext context)
    {
        _context = context;
    }

    public async Task<Fund?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Funds
            .Include(f => f.Performances)
            .FirstOrDefaultAsync(f => f.Id == id, ct);
    }

    public async Task<Fund?> GetByIsinAsync(ISIN isin, CancellationToken ct = default)
    {
        return await _context.Funds
            .Include(f => f.Performances)
            .FirstOrDefaultAsync(f => f.Isin == isin, ct);
    }

    public async Task<IReadOnlyList<Fund>> GetTopFundsAsync(int count, CancellationToken ct = default)
    {
        return await _context.Funds
            .Include(f => f.Performances)
            .OrderBy(f => f.RankingPosition)
            .Where(f => f.RankingPosition > 0)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Fund>> GetByRiskLevelAsync(RiskLevel riskLevel, CancellationToken ct = default)
    {
        return await _context.Funds
            .Where(f => f.RiskLevel == riskLevel)
            .OrderBy(f => f.RankingPosition)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Fund>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        return await _context.Funds
            .Where(f => f.Category == category)
            .OrderBy(f => f.RankingPosition)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Fund fund, CancellationToken ct = default)
    {
        await _context.Funds.AddAsync(fund, ct);
    }

    public Task UpdateAsync(Fund fund, CancellationToken ct = default)
    {
        _context.Funds.Update(fund);
        return Task.CompletedTask;
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        return await _context.Funds.CountAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
