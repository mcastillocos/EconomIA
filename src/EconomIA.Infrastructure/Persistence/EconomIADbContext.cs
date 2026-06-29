using EconomIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Infrastructure.Persistence;

public class EconomIADbContext : DbContext
{
    public DbSet<Fund> Funds => Set<Fund>();
    public DbSet<FundPerformance> FundPerformances => Set<FundPerformance>();
    public DbSet<MarketSector> MarketSectors => Set<MarketSector>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Watchlist> Watchlists => Set<Watchlist>();
    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();
    public DbSet<UploadedDocument> UploadedDocuments => Set<UploadedDocument>();
    public DbSet<FinancialMetric> FinancialMetrics => Set<FinancialMetric>();
    public DbSet<AIReport> AIReports => Set<AIReport>();
    public DbSet<AgentRun> AgentRuns => Set<AgentRun>();
    public DbSet<ChecklistTemplate> ChecklistTemplates => Set<ChecklistTemplate>();
    public DbSet<ChecklistTemplateItem> ChecklistTemplateItems => Set<ChecklistTemplateItem>();
    public DbSet<ChecklistInstance> ChecklistInstances => Set<ChecklistInstance>();
    public DbSet<ChecklistAnswer> ChecklistAnswers => Set<ChecklistAnswer>();
    public DbSet<EarningsCall> EarningsCalls => Set<EarningsCall>();
    public DbSet<InvestorProfile> InvestorProfiles => Set<InvestorProfile>();
    public DbSet<ScreenerRecommendation> ScreenerRecommendations => Set<ScreenerRecommendation>();
    public DbSet<Alert> Alerts => Set<Alert>();

    public EconomIADbContext(DbContextOptions<EconomIADbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EconomIADbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
