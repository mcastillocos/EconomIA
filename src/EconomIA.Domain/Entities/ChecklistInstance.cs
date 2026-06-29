namespace EconomIA.Domain.Entities;

/// <summary>
/// Instancia de un checklist rellenado para una entidad específica (empresa/fondo).
/// Vincula un template con respuestas concretas del usuario.
/// </summary>
public class ChecklistInstance : Entity<Guid>
{
    public Guid TemplateId { get; private set; }
    public string EntityType { get; private set; } = string.Empty; // company | fund
    public string EntityName { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public string Status { get; private set; } = "in_progress"; // in_progress | completed
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<ChecklistAnswer> _answers = [];
    public IReadOnlyList<ChecklistAnswer> Answers => _answers.AsReadOnly();

    private ChecklistInstance() { }

    public static ChecklistInstance Create(Guid templateId, string entityType, string entityName, Guid? entityId = null)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new Exceptions.DomainException("Entity name cannot be empty.");

        return new ChecklistInstance
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            EntityType = entityType,
            EntityName = entityName,
            EntityId = entityId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public ChecklistAnswer SetAnswer(Guid templateItemId, string value, string? comment = null)
    {
        var existing = _answers.FirstOrDefault(a => a.TemplateItemId == templateItemId);
        if (existing is not null)
            _answers.Remove(existing);

        var answer = new ChecklistAnswer
        {
            Id = Guid.NewGuid(),
            InstanceId = Id,
            TemplateItemId = templateItemId,
            Value = value,
            Comment = comment,
            AnsweredAt = DateTime.UtcNow,
        };

        _answers.Add(answer);
        UpdatedAt = DateTime.UtcNow;
        return answer;
    }

    public void MarkCompleted(string? notes = null)
    {
        Status = "completed";
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        Status = "in_progress";
        UpdatedAt = DateTime.UtcNow;
    }

    public int CompletionPercentage(int totalItems)
    {
        if (totalItems == 0) return 0;
        return (int)Math.Round((double)_answers.Count / totalItems * 100);
    }
}

public class ChecklistAnswer
{
    public Guid Id { get; init; }
    public Guid InstanceId { get; init; }
    public Guid TemplateItemId { get; init; }
    public string Value { get; init; } = string.Empty; // "true"/"false", "1-5", text, number
    public string? Comment { get; init; }
    public DateTime AnsweredAt { get; init; }
}
