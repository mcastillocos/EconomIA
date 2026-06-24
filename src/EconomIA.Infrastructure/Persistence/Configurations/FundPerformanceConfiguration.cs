using EconomIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Infrastructure.Persistence.Configurations;

public class FundPerformanceConfiguration : IEntityTypeConfiguration<FundPerformance>
{
    public void Configure(EntityTypeBuilder<FundPerformance> builder)
    {
        builder.ToTable("FundPerformances");
        builder.HasKey(p => p.Id);

        builder.OwnsOne(p => p.Return1Month, b => b.Property(x => x.Value).HasColumnName("Return1Month").HasPrecision(8, 4));
        builder.OwnsOne(p => p.Return3Months, b => b.Property(x => x.Value).HasColumnName("Return3Months").HasPrecision(8, 4));
        builder.OwnsOne(p => p.Return6Months, b => b.Property(x => x.Value).HasColumnName("Return6Months").HasPrecision(8, 4));
        builder.OwnsOne(p => p.Return1Year, b => b.Property(x => x.Value).HasColumnName("Return1Year").HasPrecision(8, 4));
        builder.OwnsOne(p => p.Return3Years, b => b.Property(x => x.Value).HasColumnName("Return3Years").HasPrecision(8, 4));
        builder.OwnsOne(p => p.Return5Years, b => b.Property(x => x.Value).HasColumnName("Return5Years").HasPrecision(8, 4));
        builder.OwnsOne(p => p.Volatility, b => b.Property(x => x.Value).HasColumnName("Volatility").HasPrecision(8, 4));

        builder.Property(p => p.SharpeRatio).HasPrecision(8, 4);
        builder.Property(p => p.RecordedAt);
        builder.Property(p => p.FundId);

        builder.HasIndex(p => p.FundId);
        builder.HasIndex(p => p.RecordedAt);
    }
}
