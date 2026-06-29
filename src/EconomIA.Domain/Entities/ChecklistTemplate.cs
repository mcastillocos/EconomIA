namespace EconomIA.Domain.Entities;

/// <summary>
/// Template de checklist configurable. Define una plantilla reutilizable
/// con items de verificación para análisis de inversiones.
/// </summary>
public class ChecklistTemplate : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Category { get; private set; } = string.Empty; // company | fund | portfolio | general
    public bool IsBuiltIn { get; private set; } // Templates predefinidos no se pueden borrar
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<ChecklistTemplateItem> _items = [];
    public IReadOnlyList<ChecklistTemplateItem> Items => _items.AsReadOnly();

    private ChecklistTemplate() { }

    public static ChecklistTemplate Create(string name, string category, string? description = null, bool isBuiltIn = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Exceptions.DomainException("Checklist template name cannot be empty.");
        if (string.IsNullOrWhiteSpace(category))
            throw new Exceptions.DomainException("Checklist category cannot be empty.");

        return new ChecklistTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = category,
            Description = description,
            IsBuiltIn = isBuiltIn,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public ChecklistTemplateItem AddItem(string text, string section, int order, string itemType = "boolean", string? helpText = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new Exceptions.DomainException("Checklist item text cannot be empty.");

        var item = new ChecklistTemplateItem
        {
            Id = Guid.NewGuid(),
            TemplateId = Id,
            Text = text,
            Section = section,
            Order = order,
            ItemType = itemType,
            HelpText = helpText,
        };

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

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Exceptions.DomainException("Checklist template name cannot be empty.");

        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class ChecklistTemplateItem
{
    public Guid Id { get; init; }
    public Guid TemplateId { get; init; }
    public string Text { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty; // Sección/grupo del item
    public int Order { get; init; }
    public string ItemType { get; init; } = "boolean"; // boolean | rating | text | numeric
    public string? HelpText { get; init; } // Ayuda contextual para el usuario
}
