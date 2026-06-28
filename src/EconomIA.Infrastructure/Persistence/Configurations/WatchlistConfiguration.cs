using EconomIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Infrastructure.Persistence.Configurations;

public class WatchlistConfiguration : IEntityTypeConfiguration<Watchlist>
{
    public void Configure(EntityTypeBuilder<Watchlist> builder)
    {
        builder.ToTable("Watchlists");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(1000);
        builder.Property(w => w.CreatedAt);
        builder.Property(w => w.UpdatedAt);

        builder.HasMany(w => w.Items)
            .WithOne()
            .HasForeignKey(i => i.WatchlistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(w => w.DomainEvents);
    }
}

public class WatchlistItemConfiguration : IEntityTypeConfiguration<WatchlistItem>
{
    public void Configure(EntityTypeBuilder<WatchlistItem> builder)
    {
        builder.ToTable("WatchlistItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(i => i.EntityId);
        builder.Property(i => i.Priority);
        builder.Property(i => i.PositionType).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Thesis);
        builder.Property(i => i.Notes);
        builder.Property(i => i.CreatedAt);

        builder.HasIndex(i => new { i.EntityType, i.EntityId });

        builder.Ignore(i => i.DomainEvents);
    }
}
