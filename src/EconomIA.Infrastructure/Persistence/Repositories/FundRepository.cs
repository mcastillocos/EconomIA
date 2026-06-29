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

    public async Task<(IReadOnlyList<Fund> Funds, int TotalCount)> GetFilteredAsync(
        RiskLevel? riskLevel = null,
        string? category = null,
        string? managementCompany = null,
        FundRating? minRating = null,
        decimal? maxExpenseRatio = null,
        decimal? minReturn1Year = null,
        decimal? maxVolatility = null,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = _context.Funds
            .Include(f => f.Performances)
            .AsQueryable();

        if (riskLevel.HasValue)
            query = query.Where(f => f.RiskLevel == riskLevel.Value);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(f => f.Category == category);

        if (!string.IsNullOrWhiteSpace(managementCompany))
            query = query.Where(f => f.ManagementCompany == managementCompany);

        if (minRating.HasValue)
            query = query.Where(f => f.Rating >= minRating.Value);

        if (maxExpenseRatio.HasValue)
            query = query.Where(f => f.ExpenseRatio.Value <= maxExpenseRatio.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            query = query.Where(f =>
                f.Name.ToLower().Contains(term) ||
                f.Isin.Value.ToLower().Contains(term) ||
                f.ManagementCompany.ToLower().Contains(term));
        }

        // Performance-based filters require client evaluation for owned Percentage type
        if (minReturn1Year.HasValue || maxVolatility.HasValue)
        {
            var materialized = await query.ToListAsync(ct);

            if (minReturn1Year.HasValue)
                materialized = materialized
                    .Where(f => f.Performances.Any() &&
                        f.Performances.OrderByDescending(p => p.RecordedAt).First().Return1Year.Value >= minReturn1Year.Value)
                    .ToList();

            if (maxVolatility.HasValue)
                materialized = materialized
                    .Where(f => f.Performances.Any() &&
                        f.Performances.OrderByDescending(p => p.RecordedAt).First().Volatility.Value <= maxVolatility.Value)
                    .ToList();

            var totalCount = materialized.Count;
            var sorted = ApplySortingInMemory(materialized, sortBy, sortDescending);
            var paged = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return (paged, totalCount);
        }

        var dbTotalCount = await query.CountAsync(ct);
        var dbSorted = ApplySortingQueryable(query, sortBy, sortDescending);
        var dbPaged = await dbSorted.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (dbPaged, dbTotalCount);
    }

    public async Task<IReadOnlyList<string>> GetDistinctCategoriesAsync(CancellationToken ct = default)
    {
        return await _context.Funds
            .Select(f => f.Category)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetDistinctManagementCompaniesAsync(CancellationToken ct = default)
    {
        return await _context.Funds
            .Select(f => f.ManagementCompany)
            .Where(m => !string.IsNullOrEmpty(m))
            .Distinct()
            .OrderBy(m => m)
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

    private static IQueryable<Fund> ApplySortingQueryable(IQueryable<Fund> query, string? sortBy, bool desc)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "name" => desc ? query.OrderByDescending(f => f.Name) : query.OrderBy(f => f.Name),
            "expenseratio" => desc ? query.OrderByDescending(f => f.ExpenseRatio.Value) : query.OrderBy(f => f.ExpenseRatio.Value),
            "rating" => desc ? query.OrderByDescending(f => f.Rating) : query.OrderBy(f => f.Rating),
            "netassetvalue" => desc ? query.OrderByDescending(f => f.NetAssetValue.Amount) : query.OrderBy(f => f.NetAssetValue.Amount),
            _ => desc ? query.OrderByDescending(f => f.RankingPosition) : query.OrderBy(f => f.RankingPosition),
        };
    }

    private static IEnumerable<Fund> ApplySortingInMemory(IEnumerable<Fund> funds, string? sortBy, bool desc)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "name" => desc ? funds.OrderByDescending(f => f.Name) : funds.OrderBy(f => f.Name),
            "return1year" => desc
                ? funds.OrderByDescending(f => f.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault()?.Return1Year.Value ?? 0)
                : funds.OrderBy(f => f.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault()?.Return1Year.Value ?? 0),
            "expenseratio" => desc ? funds.OrderByDescending(f => f.ExpenseRatio.Value) : funds.OrderBy(f => f.ExpenseRatio.Value),
            "rating" => desc ? funds.OrderByDescending(f => f.Rating) : funds.OrderBy(f => f.Rating),
            "volatility" => desc
                ? funds.OrderByDescending(f => f.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault()?.Volatility.Value ?? 0)
                : funds.OrderBy(f => f.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault()?.Volatility.Value ?? 0),
            "netassetvalue" => desc ? funds.OrderByDescending(f => f.NetAssetValue.Amount) : funds.OrderBy(f => f.NetAssetValue.Amount),
            _ => desc ? funds.OrderByDescending(f => f.RankingPosition) : funds.OrderBy(f => f.RankingPosition),
        };
    }
}
