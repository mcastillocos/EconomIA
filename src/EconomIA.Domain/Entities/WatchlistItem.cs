namespace EconomIA.Domain.Entities;

public class WatchlistItem : Entity<Guid>
{
    public Guid WatchlistId { get; private set; }
    public string EntityType { get; private set; } = string.Empty; // "fund" | "company"
    public Guid EntityId { get; private set; }
    public int Priority { get; private set; }
    public string PositionType { get; private set; } = "watch"; // "real" | "watch"
    public string? Thesis { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private WatchlistItem() { }

    public static WatchlistItem Create(
        Guid watchlistId,
        string entityType,
        Guid entityId,
        int priority = 0,
        string positionType = "watch",
        string? thesis = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new Exceptions.DomainException("Entity type cannot be empty.");

        return new WatchlistItem
        {
            Id = Guid.NewGuid(),
            WatchlistId = watchlistId,
            EntityType = entityType,
            EntityId = entityId,
            Priority = priority,
            PositionType = positionType,
            Thesis = thesis,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDetails(int priority, string positionType, string? thesis, string? notes)
    {
        Priority = priority;
        PositionType = positionType;
        Thesis = thesis;
        Notes = notes;
    }
}
