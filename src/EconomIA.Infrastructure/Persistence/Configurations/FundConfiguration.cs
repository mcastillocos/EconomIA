using EconomIA.Domain.Entities;
using EconomIA.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Infrastructure.Persistence.Configurations;

public class FundConfiguration : IEntityTypeConfiguration<Fund>
{
    public void Configure(EntityTypeBuilder<Fund> builder)
    {
        builder.ToTable("Funds");
        builder.HasKey(f => f.Id);

        builder.OwnsOne(f => f.Isin, isin =>
        {
            isin.Property(i => i.Value)
                .HasColumnName("Isin")
                .HasMaxLength(12)
                .IsRequired();
            isin.HasIndex(i => i.Value).IsUnique();
        });

        builder.OwnsOne(f => f.NetAssetValue, nav =>
        {
            nav.Property(m => m.Amount).HasColumnName("NetAssetValue").HasPrecision(18, 6);
            nav.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
        });

        builder.OwnsOne(f => f.ExpenseRatio, er =>
        {
            er.Property(p => p.Value).HasColumnName("ExpenseRatio").HasPrecision(8, 4);
        });

        builder.Property(f => f.Name).HasMaxLength(500).IsRequired();
        builder.Property(f => f.Category).HasMaxLength(200);
        builder.Property(f => f.ManagementCompany).HasMaxLength(300);
        builder.Property(f => f.RiskLevel).HasConversion<int>();
        builder.Property(f => f.Rating).HasConversion<int>();
        builder.Property(f => f.RankingPosition);
        builder.Property(f => f.LastUpdated);

        builder.HasMany(f => f.Performances)
            .WithOne()
            .HasForeignKey(p => p.FundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(f => f.DomainEvents);

        builder.HasIndex(f => f.RankingPosition);
        builder.HasIndex(f => f.RiskLevel);
    }
}
