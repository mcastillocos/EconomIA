using EconomIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Infrastructure.Persistence.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Field).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Operator).HasMaxLength(10).IsRequired();
        builder.Property(a => a.Threshold).HasColumnType("decimal(18,4)");
        builder.Property(a => a.Condition).HasMaxLength(500);
        builder.Property(a => a.LastMessage).HasMaxLength(1000);
    }
}
