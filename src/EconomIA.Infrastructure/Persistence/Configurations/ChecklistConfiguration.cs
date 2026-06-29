using EconomIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Infrastructure.Persistence.Configurations;

public class ChecklistTemplateConfiguration : IEntityTypeConfiguration<ChecklistTemplate>
{
    public void Configure(EntityTypeBuilder<ChecklistTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(1000);
        builder.Property(t => t.Category).HasMaxLength(50).IsRequired();

        builder.HasMany(t => t.Items)
            .WithOne()
            .HasForeignKey(i => i.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class ChecklistTemplateItemConfiguration : IEntityTypeConfiguration<ChecklistTemplateItem>
{
    public void Configure(EntityTypeBuilder<ChecklistTemplateItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Text).HasMaxLength(500).IsRequired();
        builder.Property(i => i.Section).HasMaxLength(100).IsRequired();
        builder.Property(i => i.ItemType).HasMaxLength(20).IsRequired();
        builder.Property(i => i.HelpText).HasMaxLength(500);
        builder.Property(i => i.Order).HasColumnName("Order");
    }
}

public class ChecklistInstanceConfiguration : IEntityTypeConfiguration<ChecklistInstance>
{
    public void Configure(EntityTypeBuilder<ChecklistInstance> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(i => i.EntityName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Status).HasMaxLength(20).IsRequired();

        builder.HasMany(i => i.Answers)
            .WithOne()
            .HasForeignKey(a => a.InstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(i => i.Answers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class ChecklistAnswerConfiguration : IEntityTypeConfiguration<ChecklistAnswer>
{
    public void Configure(EntityTypeBuilder<ChecklistAnswer> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Value).HasMaxLength(500).IsRequired();
        builder.Property(a => a.Comment).HasMaxLength(1000);
    }
}
