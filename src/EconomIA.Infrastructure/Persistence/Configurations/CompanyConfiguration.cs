using EconomIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(500).IsRequired();
        builder.Property(c => c.Ticker).HasMaxLength(20);
        builder.Property(c => c.Isin).HasMaxLength(12);
        builder.Property(c => c.Market).HasMaxLength(100);
        builder.Property(c => c.Country).HasMaxLength(100);
        builder.Property(c => c.Sector).HasMaxLength(200);
        builder.Property(c => c.Industry).HasMaxLength(200);
        builder.Property(c => c.Currency).HasMaxLength(3);
        builder.Property(c => c.Competitors);
        builder.Property(c => c.RelevantUrls);
        builder.Property(c => c.PreferredSource).HasMaxLength(200);
        builder.Property(c => c.Notes);
        builder.Property(c => c.CreatedAt);
        builder.Property(c => c.UpdatedAt);

        builder.HasIndex(c => c.Ticker);
        builder.HasIndex(c => c.Isin);
        builder.HasIndex(c => c.Sector);
        builder.HasIndex(c => c.Country);

        builder.Ignore(c => c.DomainEvents);
    }
}
