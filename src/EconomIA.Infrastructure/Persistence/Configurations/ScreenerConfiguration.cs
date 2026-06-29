using EconomIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Infrastructure.Persistence.Configurations;

public class EarningsCallConfiguration : IEntityTypeConfiguration<EarningsCall>
{
    public void Configure(EntityTypeBuilder<EarningsCall> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Ticker).HasMaxLength(20);
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Sentiment).HasMaxLength(20);
        builder.Property(e => e.ErrorMessage).HasMaxLength(1000);
        builder.Property(e => e.AudioFilePath).HasMaxLength(500);
        builder.Property(e => e.Language).HasMaxLength(10);

        builder.HasIndex(e => e.CompanyName);
    }
}

public class InvestorProfileConfiguration : IEntityTypeConfiguration<InvestorProfile>
{
    public void Configure(EntityTypeBuilder<InvestorProfile> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.UserId).HasMaxLength(100).IsRequired();
        builder.Property(p => p.RiskTolerance).HasMaxLength(20);
        builder.Property(p => p.InvestmentHorizon).HasMaxLength(20);
        builder.Property(p => p.InvestmentStyle).HasMaxLength(20);
        builder.Property(p => p.AssetPreference).HasMaxLength(20);
        builder.Property(p => p.MaxExpenseRatio).HasColumnType("decimal(5,2)");
        builder.Property(p => p.MinReturn1Y).HasColumnType("decimal(8,2)");

        builder.HasIndex(p => p.UserId).IsUnique();
    }
}

public class ScreenerRecommendationConfiguration : IEntityTypeConfiguration<ScreenerRecommendation>
{
    public void Configure(EntityTypeBuilder<ScreenerRecommendation> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(r => r.EntityName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Ticker).HasMaxLength(20);
        builder.Property(r => r.Isin).HasMaxLength(20);
        builder.Property(r => r.Category).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Status).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Score).HasColumnType("decimal(5,2)");

        builder.HasIndex(r => r.ProfileId);
    }
}
