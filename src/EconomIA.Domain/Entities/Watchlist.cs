namespace EconomIA.Domain.Entities;

public class Watchlist : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<WatchlistItem> _items = [];
    public IReadOnlyList<WatchlistItem> Items => _items.AsReadOnly();

    private Watchlist() { }

    public static Watchlist Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Exceptions.DomainException("Watchlist name cannot be empty.");

        return new Watchlist
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Exceptions.DomainException("Watchlist name cannot be empty.");

        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public WatchlistItem AddItem(string entityType, Guid entityId, int priority = 0, string positionType = "watch", string? thesis = null, string? notes = null)
    {
        var item = WatchlistItem.Create(Id, entityType, entityId, priority, positionType, thesis, notes);
        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is not null)
        {
            _items.Remove(item);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
