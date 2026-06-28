using EconomIA.Domain.Entities;

namespace EconomIA.Domain.Ports;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Company>> GetBySectorAsync(string sector, CancellationToken ct = default);
    Task<IReadOnlyList<Company>> GetByCountryAsync(string country, CancellationToken ct = default);
    Task<Company?> GetByTickerAsync(string ticker, CancellationToken ct = default);
    Task AddAsync(Company company, CancellationToken ct = default);
    Task UpdateAsync(Company company, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
