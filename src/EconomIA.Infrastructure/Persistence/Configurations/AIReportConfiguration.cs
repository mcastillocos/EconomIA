using EconomIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EconomIA.Infrastructure.Persistence.Configurations;

public class AIReportConfiguration : IEntityTypeConfiguration<AIReport>
{
    public void Configure(EntityTypeBuilder<AIReport> builder)
    {
        builder.ToTable("AIReports");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(r => r.EntityId);
        builder.Property(r => r.ReportType).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Title).HasMaxLength(500).IsRequired();
        builder.Property(r => r.Content);
        builder.Property(r => r.Sources);
        builder.Property(r => r.Confidence).HasMaxLength(20);
        builder.Property(r => r.CreatedAt);

        builder.HasIndex(r => new { r.EntityType, r.EntityId });
        builder.HasIndex(r => r.ReportType);

        builder.Ignore(r => r.DomainEvents);
    }
}

public class AgentRunConfiguration : IEntityTypeConfiguration<AgentRun>
{
    public void Configure(EntityTypeBuilder<AgentRun> builder)
    {
        builder.ToTable("AgentRuns");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AgentName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Status).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Input);
        builder.Property(a => a.Output);
        builder.Property(a => a.Sources);
        builder.Property(a => a.Error);
        builder.Property(a => a.StartedAt);
        builder.Property(a => a.CompletedAt);

        builder.HasIndex(a => a.AgentName);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.StartedAt).IsDescending();

        builder.Ignore(a => a.DomainEvents);
    }
}
