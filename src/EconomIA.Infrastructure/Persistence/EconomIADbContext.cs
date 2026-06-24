using EconomIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Infrastructure.Persistence;

public class EconomIADbContext : DbContext
{
    public DbSet<Fund> Funds => Set<Fund>();
    public DbSet<FundPerformance> FundPerformances => Set<FundPerformance>();
    public DbSet<MarketSector> MarketSectors => Set<MarketSector>();

    public EconomIADbContext(DbContextOptions<EconomIADbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EconomIADbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
