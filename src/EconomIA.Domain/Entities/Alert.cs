namespace EconomIA.Domain.Entities;

public class Alert : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty; // "fund", "company", "metric"
    public Guid? EntityId { get; private set; }
    public string Condition { get; private set; } = string.Empty; // "return1y < 0", "rating < 3", "sharpe < 1"
    public string Operator { get; private set; } = string.Empty; // "<", ">", "==", "<=", ">="
    public decimal Threshold { get; private set; }
    public string Field { get; private set; } = string.Empty; // "return1y", "rating", "sharpe", "ter", "nav"
    public bool IsActive { get; private set; } = true;
    public bool HasTriggered { get; private set; }
    public DateTime? LastTriggeredAt { get; private set; }
    public string? LastMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Alert() { }

    public static Alert Create(string name, string entityType, Guid? entityId, string field, string op, decimal threshold)
    {
        return new Alert
        {
            Id = Guid.NewGuid(),
            Name = name,
            EntityType = entityType,
            EntityId = entityId,
            Field = field,
            Operator = op,
            Threshold = threshold,
            Condition = $"{field} {op} {threshold}",
            IsActive = true,
            HasTriggered = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Trigger(string message)
    {
        HasTriggered = true;
        LastTriggeredAt = DateTime.UtcNow;
        LastMessage = message;
    }

    public void Reset()
    {
        HasTriggered = false;
        LastMessage = null;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
