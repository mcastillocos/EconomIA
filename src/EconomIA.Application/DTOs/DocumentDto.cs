namespace EconomIA.Application.DTOs;

public record DocumentDto(
    Guid Id,
    string EntityType,
    Guid? EntityId,
    string FileName,
    string FileType,
    string? Source,
    string FilePath,
    long FileSize,
    DateTime UploadDate,
    string Status,
    string? Summary,
    string? ErrorMessage);

public record UploadDocumentRequest(
    string EntityType,
    Guid? EntityId,
    string? Source);
