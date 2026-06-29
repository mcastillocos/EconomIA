namespace EconomIA.Domain.Entities;

/// <summary>
/// Perfil de inversión del usuario. Se construye y actualiza automáticamente
/// a partir de sus acciones (watchlists, checklists completados, búsquedas, etc.)
/// y de un cuestionario inicial opcional.
/// </summary>
public class InvestorProfile : Entity<Guid>
{
    public string UserId { get; private set; } = "default"; // Para multi-user futuro
    public string RiskTolerance { get; private set; } = "moderate"; // conservative | moderate | aggressive
    public string InvestmentHorizon { get; private set; } = "medium"; // short | medium | long
    public decimal MaxExpenseRatio { get; private set; } = 1.5m; // TER máximo aceptable (%)
    public decimal MinReturn1Y { get; private set; } = 0m; // Rentabilidad mínima a 1 año (%)
    public string PreferredSectors { get; private set; } = string.Empty; // JSON array: ["Technology","Healthcare"]
    public string PreferredGeographies { get; private set; } = string.Empty; // JSON array: ["USA","Europe"]
    public string ExcludedSectors { get; private set; } = string.Empty; // JSON array
    public string InvestmentStyle { get; private set; } = "blend"; // value | growth | blend | income | momentum
    public string AssetPreference { get; private set; } = "both"; // funds | stocks | both
    public bool EsgPreference { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public int InteractionsCount { get; private set; }

    private InvestorProfile() { }

    public static InvestorProfile Create(string userId = "default")
    {
        return new InvestorProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void UpdateFromQuestionnaire(
        string riskTolerance,
        string investmentHorizon,
        string investmentStyle,
        string assetPreference,
        decimal maxExpenseRatio,
        decimal minReturn1Y,
        bool esgPreference,
        string preferredSectors,
        string preferredGeographies,
        string excludedSectors)
    {
        RiskTolerance = ValidateEnum(riskTolerance, ["conservative", "moderate", "aggressive"], "moderate");
        InvestmentHorizon = ValidateEnum(investmentHorizon, ["short", "medium", "long"], "medium");
        InvestmentStyle = ValidateEnum(investmentStyle, ["value", "growth", "blend", "income", "momentum"], "blend");
        AssetPreference = ValidateEnum(assetPreference, ["funds", "stocks", "both"], "both");
        MaxExpenseRatio = Math.Clamp(maxExpenseRatio, 0, 10);
        MinReturn1Y = Math.Clamp(minReturn1Y, -100, 1000);
        EsgPreference = esgPreference;
        PreferredSectors = preferredSectors;
        PreferredGeographies = preferredGeographies;
        ExcludedSectors = excludedSectors;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordInteraction()
    {
        InteractionsCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void LearnFromAction(string actionType, string details)
    {
        // Ajustes automáticos basados en comportamiento
        InteractionsCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string ValidateEnum(string value, string[] valid, string fallback)
        => valid.Contains(value.ToLowerInvariant()) ? value.ToLowerInvariant() : fallback;
}
