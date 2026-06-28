using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Infrastructure.Persistence.Repositories;

public class FinancialMetricRepository : IFinancialMetricRepository
{
    private readonly EconomIADbContext _context;

    public FinancialMetricRepository(EconomIADbContext context)
    {
        _context = context;
    }

    public async Task<FinancialMetric?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.FinancialMetrics.FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<IReadOnlyList<FinancialMetric>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.FinancialMetrics.OrderByDescending(m => m.CreatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FinancialMetric>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
    {
        return await _context.FinancialMetrics
            .Where(m => m.EntityType == entityType && m.EntityId == entityId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FinancialMetric>> GetByMetricNameAsync(string metricName, CancellationToken ct = default)
    {
        return await _context.FinancialMetrics
            .Where(m => m.MetricName == metricName)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FinancialMetric>> GetFilteredAsync(
        string? entityType = null,
        Guid? entityId = null,
        string? metricName = null,
        int? year = null,
        int? quarter = null,
        string? source = null,
        bool? validated = null,
        CancellationToken ct = default)
    {
        var query = _context.FinancialMetrics.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(m => m.EntityType == entityType);

        if (entityId.HasValue)
            query = query.Where(m => m.EntityId == entityId.Value);

        if (!string.IsNullOrWhiteSpace(metricName))
            query = query.Where(m => m.MetricName.Contains(metricName));

        if (year.HasValue)
            query = query.Where(m => m.Year == year.Value);

        if (quarter.HasValue)
            query = query.Where(m => m.Quarter == quarter.Value);

        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(m => m.Source == source);

        if (validated.HasValue)
            query = query.Where(m => m.Validated == validated.Value);

        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync(ct);
    }

    public async Task AddAsync(FinancialMetric metric, CancellationToken ct = default)
    {
        await _context.FinancialMetrics.AddAsync(metric, ct);
    }

    public async Task AddRangeAsync(IEnumerable<FinancialMetric> metrics, CancellationToken ct = default)
    {
        await _context.FinancialMetrics.AddRangeAsync(metrics, ct);
    }

    public Task UpdateAsync(FinancialMetric metric, CancellationToken ct = default)
    {
        _context.FinancialMetrics.Update(metric);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
