using EconomIA.Domain.Entities;

namespace EconomIA.Domain.Ports;

public interface IFinancialMetricRepository
{
    Task<FinancialMetric?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<FinancialMetric>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FinancialMetric>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
    Task<IReadOnlyList<FinancialMetric>> GetByMetricNameAsync(string metricName, CancellationToken ct = default);
    Task<IReadOnlyList<FinancialMetric>> GetFilteredAsync(
        string? entityType = null,
        Guid? entityId = null,
        string? metricName = null,
        int? year = null,
        int? quarter = null,
        string? source = null,
        bool? validated = null,
        CancellationToken ct = default);
    Task AddAsync(FinancialMetric metric, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<FinancialMetric> metrics, CancellationToken ct = default);
    Task UpdateAsync(FinancialMetric metric, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
