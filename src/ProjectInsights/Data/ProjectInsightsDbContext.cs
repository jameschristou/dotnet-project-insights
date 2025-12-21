using Microsoft.EntityFrameworkCore;
using ProjectInsights.Data.Configurations;
using ProjectInsights.Data.Entities;

namespace ProjectInsights.Data;

public class ProjectInsightsDbContext : DbContext
{
    public ProjectInsightsDbContext(DbContextOptions<ProjectInsightsDbContext> options)
        : base(options)
    {
    }

    public DbSet<AnalysisRun> AnalysisRuns => Set<AnalysisRun>();
    public DbSet<PullRequest> PullRequests => Set<PullRequest>();
    public DbSet<PrFile> PrFiles => Set<PrFile>();
    public DbSet<PrProject> PrProjects => Set<PrProject>();
    public DbSet<DailyProjectStats> DailyProjectStats => Set<DailyProjectStats>();
    public DbSet<DailyTeamProjectStats> DailyTeamProjectStats => Set<DailyTeamProjectStats>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new AnalysisRunConfiguration());
        modelBuilder.ApplyConfiguration(new PullRequestConfiguration());
        modelBuilder.ApplyConfiguration(new PrFileConfiguration());
        modelBuilder.ApplyConfiguration(new PrProjectConfiguration());
        modelBuilder.ApplyConfiguration(new DailyProjectStatsConfiguration());
        modelBuilder.ApplyConfiguration(new DailyTeamProjectStatsConfiguration());
    }
}
