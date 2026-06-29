using EconomIA.Domain.Entities;
using EconomIA.Domain.ValueObjects;

namespace EconomIA.Domain.Ports;

public interface IFundRepository
{
    Task<Fund?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Fund?> GetByIsinAsync(ISIN isin, CancellationToken ct = default);
    Task<IReadOnlyList<Fund>> GetTopFundsAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<Fund>> GetByRiskLevelAsync(RiskLevel riskLevel, CancellationToken ct = default);
    Task<IReadOnlyList<Fund>> GetByCategoryAsync(string category, CancellationToken ct = default);
    Task<(IReadOnlyList<Fund> Funds, int TotalCount)> GetFilteredAsync(
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
        CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetDistinctCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetDistinctManagementCompaniesAsync(CancellationToken ct = default);
    Task AddAsync(Fund fund, CancellationToken ct = default);
    Task UpdateAsync(Fund fund, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
