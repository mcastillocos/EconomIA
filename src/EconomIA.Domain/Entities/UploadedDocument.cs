namespace EconomIA.Domain.Entities;

public class UploadedDocument : Entity<Guid>
{
    public string EntityType { get; private set; } = string.Empty; // "fund" | "company" | "sector" | "portfolio"
    public Guid? EntityId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string FileType { get; private set; } = string.Empty; // "csv" | "excel" | "pdf" | "transcript" | "audio"
    public string? Source { get; private set; }
    public string FilePath { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public DateTime UploadDate { get; private set; }
    public string Status { get; private set; } = "pending"; // "pending" | "processing" | "completed" | "failed"
    public string? ExtractedText { get; private set; }
    public string? Summary { get; private set; }
    public string? Metadata { get; private set; }
    public string? ErrorMessage { get; private set; }

    private UploadedDocument() { }

    public static UploadedDocument Create(
        string entityType,
        Guid? entityId,
        string fileName,
        string fileType,
        string filePath,
        long fileSize,
        string? source = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new Exceptions.DomainException("File name cannot be empty.");

        return new UploadedDocument
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            FileName = fileName,
            FileType = fileType,
            FilePath = filePath,
            FileSize = fileSize,
            Source = source,
            UploadDate = DateTime.UtcNow,
            Status = "pending"
        };
    }

    public void MarkProcessing()
    {
        Status = "processing";
    }

    public void MarkCompleted(string? extractedText, string? summary, string? metadata)
    {
        Status = "completed";
        ExtractedText = extractedText;
        Summary = summary;
        Metadata = metadata;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = "failed";
        ErrorMessage = errorMessage;
    }
}
