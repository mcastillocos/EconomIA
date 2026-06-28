namespace EconomIA.Domain.Entities;

public class Company : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string? Ticker { get; private set; }
    public string? Isin { get; private set; }
    public string? Market { get; private set; }
    public string? Country { get; private set; }
    public string? Sector { get; private set; }
    public string? Industry { get; private set; }
    public string? Currency { get; private set; }
    public string? Competitors { get; private set; }
    public string? RelevantUrls { get; private set; }
    public string? PreferredSource { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Company() { }

    public static Company Create(
        string name,
        string? ticker = null,
        string? isin = null,
        string? market = null,
        string? country = null,
        string? sector = null,
        string? industry = null,
        string? currency = null,
        string? competitors = null,
        string? relevantUrls = null,
        string? preferredSource = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Exceptions.DomainException("Company name cannot be empty.");

        return new Company
        {
            Id = Guid.NewGuid(),
            Name = name,
            Ticker = ticker,
            Isin = isin,
            Market = market,
            Country = country,
            Sector = sector,
            Industry = industry,
            Currency = currency,
            Competitors = competitors,
            RelevantUrls = relevantUrls,
            PreferredSource = preferredSource,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        string? ticker,
        string? isin,
        string? market,
        string? country,
        string? sector,
        string? industry,
        string? currency,
        string? competitors,
        string? relevantUrls,
        string? preferredSource,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Exceptions.DomainException("Company name cannot be empty.");

        Name = name;
        Ticker = ticker;
        Isin = isin;
        Market = market;
        Country = country;
        Sector = sector;
        Industry = industry;
        Currency = currency;
        Competitors = competitors;
        RelevantUrls = relevantUrls;
        PreferredSource = preferredSource;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
