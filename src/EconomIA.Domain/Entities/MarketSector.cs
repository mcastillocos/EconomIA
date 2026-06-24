namespace EconomIA.Domain.Entities;

public class MarketSector : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private MarketSector() { }

    public static MarketSector Create(string name, string description)
    {
        return new MarketSector
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description
        };
    }
}
