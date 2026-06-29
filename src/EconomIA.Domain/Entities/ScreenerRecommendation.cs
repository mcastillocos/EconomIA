namespace EconomIA.Domain.Entities;

/// <summary>
/// Recomendación generada por el screener inteligente basada en el perfil del usuario.
/// Se genera periódicamente o bajo demanda.
/// </summary>
public class ScreenerRecommendation : Entity<Guid>
{
    public Guid ProfileId { get; private set; }
    public string EntityType { get; private set; } = string.Empty; // fund | company
    public string EntityName { get; private set; } = string.Empty;
    public string? Ticker { get; private set; }
    public string? Isin { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public decimal Score { get; private set; } // 0-100 compatibilidad con el perfil
    public string Category { get; private set; } = string.Empty; // "core" | "tactical" | "speculative"
    public string? Metrics { get; private set; } // JSON: {"return1Y": 15.3, "ter": 0.2, ...}
    public string Status { get; private set; } = "active"; // active | dismissed | saved | invested
    public DateTime GeneratedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    private ScreenerRecommendation() { }

    public static ScreenerRecommendation Create(
        Guid profileId,
        string entityType,
        string entityName,
        string reason,
        decimal score,
        string category,
        string? ticker = null,
        string? isin = null,
        string? metrics = null)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new Exceptions.DomainException("Entity name cannot be empty for recommendation.");
        if (score < 0 || score > 100)
            throw new Exceptions.DomainException("Score must be between 0 and 100.");

        return new ScreenerRecommendation
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            EntityType = entityType,
            EntityName = entityName,
            Reason = reason,
            Score = score,
            Category = ValidateCategory(category),
            Ticker = ticker,
            Isin = isin,
            Metrics = metrics,
            GeneratedAt = DateTime.UtcNow,
        };
    }

    public void Dismiss()
    {
        Status = "dismissed";
        ReviewedAt = DateTime.UtcNow;
    }

    public void Save()
    {
        Status = "saved";
        ReviewedAt = DateTime.UtcNow;
    }

    public void MarkInvested()
    {
        Status = "invested";
        ReviewedAt = DateTime.UtcNow;
    }

    private static string ValidateCategory(string category)
        => new[] { "core", "tactical", "speculative" }.Contains(category.ToLowerInvariant())
            ? category.ToLowerInvariant()
            : "tactical";
}
