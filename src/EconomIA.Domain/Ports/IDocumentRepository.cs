using EconomIA.Domain.Entities;

namespace EconomIA.Domain.Ports;

public interface IDocumentRepository
{
    Task<UploadedDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<UploadedDocument>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UploadedDocument>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
    Task<IReadOnlyList<UploadedDocument>> GetByStatusAsync(string status, CancellationToken ct = default);
    Task AddAsync(UploadedDocument document, CancellationToken ct = default);
    Task UpdateAsync(UploadedDocument document, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
