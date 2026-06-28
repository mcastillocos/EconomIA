using EconomIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Infrastructure.Persistence.Configurations;

public class FinancialMetricConfiguration : IEntityTypeConfiguration<FinancialMetric>
{
    public void Configure(EntityTypeBuilder<FinancialMetric> builder)
    {
        builder.ToTable("FinancialMetrics");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(m => m.EntityId);
        builder.Property(m => m.Ticker).HasMaxLength(20);
        builder.Property(m => m.Isin).HasMaxLength(12);
        builder.Property(m => m.MetricName).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Value).HasPrecision(18, 6);
        builder.Property(m => m.Period).HasMaxLength(50);
        builder.Property(m => m.Year);
        builder.Property(m => m.Quarter);
        builder.Property(m => m.Currency).HasMaxLength(3);
        builder.Property(m => m.Source).HasMaxLength(200);
        builder.Property(m => m.SourceType).HasMaxLength(50);
        builder.Property(m => m.FileName).HasMaxLength(500);
        builder.Property(m => m.Page).HasMaxLength(50);
        builder.Property(m => m.Row).HasMaxLength(50);
        builder.Property(m => m.Url).HasMaxLength(2000);
        builder.Property(m => m.Confidence).HasMaxLength(20).IsRequired();
        builder.Property(m => m.RawText);
        builder.Property(m => m.Validated);
        builder.Property(m => m.ValidatedAt);
        builder.Property(m => m.CreatedAt);

        builder.HasIndex(m => new { m.EntityType, m.EntityId });
        builder.HasIndex(m => m.MetricName);
        builder.HasIndex(m => m.Ticker);
        builder.HasIndex(m => new { m.Year, m.Quarter });
        builder.HasIndex(m => m.Source);
        builder.HasIndex(m => m.Validated);

        builder.Ignore(m => m.DomainEvents);
    }
}
