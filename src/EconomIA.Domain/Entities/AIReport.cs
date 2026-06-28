namespace EconomIA.Domain.Entities;

public class AIReport : Entity<Guid>
{
    public string EntityType { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public string ReportType { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Content { get; private set; }
    public string? Sources { get; private set; }
    public string? Confidence { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AIReport() { }

    public static AIReport Create(
        string entityType,
        Guid? entityId,
        string reportType,
        string title,
        string? content = null,
        string? sources = null,
        string? confidence = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new Exceptions.DomainException("Report title cannot be empty.");

        return new AIReport
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            ReportType = reportType,
            Title = title,
            Content = content,
            Sources = sources,
            Confidence = confidence,
            CreatedAt = DateTime.UtcNow
        };
    }
}
