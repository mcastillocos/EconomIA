using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Infrastructure.Persistence.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly EconomIADbContext _context;

    public DocumentRepository(EconomIADbContext context)
    {
        _context = context;
    }

    public async Task<UploadedDocument?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.UploadedDocuments.FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<IReadOnlyList<UploadedDocument>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.UploadedDocuments.OrderByDescending(d => d.UploadDate).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<UploadedDocument>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
    {
        return await _context.UploadedDocuments
            .Where(d => d.EntityType == entityType && d.EntityId == entityId)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<UploadedDocument>> GetByStatusAsync(string status, CancellationToken ct = default)
    {
        return await _context.UploadedDocuments
            .Where(d => d.Status == status)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync(ct);
    }

    public async Task AddAsync(UploadedDocument document, CancellationToken ct = default)
    {
        await _context.UploadedDocuments.AddAsync(document, ct);
    }

    public Task UpdateAsync(UploadedDocument document, CancellationToken ct = default)
    {
        _context.UploadedDocuments.Update(document);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
