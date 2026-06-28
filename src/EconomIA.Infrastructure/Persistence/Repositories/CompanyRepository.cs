using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Infrastructure.Persistence.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly EconomIADbContext _context;

    public CompanyRepository(EconomIADbContext context)
    {
        _context = context;
    }

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Companies.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Company>> GetBySectorAsync(string sector, CancellationToken ct = default)
    {
        return await _context.Companies.Where(c => c.Sector == sector).OrderBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Company>> GetByCountryAsync(string country, CancellationToken ct = default)
    {
        return await _context.Companies.Where(c => c.Country == country).OrderBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<Company?> GetByTickerAsync(string ticker, CancellationToken ct = default)
    {
        return await _context.Companies.FirstOrDefaultAsync(c => c.Ticker == ticker, ct);
    }

    public async Task AddAsync(Company company, CancellationToken ct = default)
    {
        await _context.Companies.AddAsync(company, ct);
    }

    public Task UpdateAsync(Company company, CancellationToken ct = default)
    {
        _context.Companies.Update(company);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (company is not null)
            _context.Companies.Remove(company);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
