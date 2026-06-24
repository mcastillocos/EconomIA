using EconomIA.Domain.ValueObjects;

namespace EconomIA.Domain.Entities;

public class FundPerformance : Entity<Guid>
{
    public Guid FundId { get; private set; }
    public Percentage Return1Month { get; private set; } = null!;
    public Percentage Return3Months { get; private set; } = null!;
    public Percentage Return6Months { get; private set; } = null!;
    public Percentage Return1Year { get; private set; } = null!;
    public Percentage Return3Years { get; private set; } = null!;
    public Percentage Return5Years { get; private set; } = null!;
    public Percentage Volatility { get; private set; } = null!;
    public decimal SharpeRatio { get; private set; }
    public DateTime RecordedAt { get; private set; }

    private FundPerformance() { }

    public static FundPerformance Create(
        Guid fundId,
        Percentage return1m,
        Percentage return3m,
        Percentage return6m,
        Percentage return1y,
        Percentage return3y,
        Percentage return5y,
        Percentage volatility,
        decimal sharpeRatio)
    {
        return new FundPerformance
        {
            Id = Guid.NewGuid(),
            FundId = fundId,
            Return1Month = return1m,
            Return3Months = return3m,
            Return6Months = return6m,
            Return1Year = return1y,
            Return3Years = return3y,
            Return5Years = return5y,
            Volatility = volatility,
            SharpeRatio = sharpeRatio,
            RecordedAt = DateTime.UtcNow
        };
    }
}
