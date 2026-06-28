using EconomIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Infrastructure.Persistence.Configurations;

public class UploadedDocumentConfiguration : IEntityTypeConfiguration<UploadedDocument>
{
    public void Configure(EntityTypeBuilder<UploadedDocument> builder)
    {
        builder.ToTable("UploadedDocuments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(d => d.EntityId);
        builder.Property(d => d.FileName).HasMaxLength(500).IsRequired();
        builder.Property(d => d.FileType).HasMaxLength(50).IsRequired();
        builder.Property(d => d.Source).HasMaxLength(200);
        builder.Property(d => d.FilePath).HasMaxLength(1000).IsRequired();
        builder.Property(d => d.FileSize);
        builder.Property(d => d.UploadDate);
        builder.Property(d => d.Status).HasMaxLength(50).IsRequired();
        builder.Property(d => d.ExtractedText);
        builder.Property(d => d.Summary);
        builder.Property(d => d.Metadata);
        builder.Property(d => d.ErrorMessage);

        builder.HasIndex(d => new { d.EntityType, d.EntityId });
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.UploadDate).IsDescending();

        builder.Ignore(d => d.DomainEvents);
    }
}
