using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectInsights.Data.Entities;

namespace ProjectInsights.Data.Configurations;

public class AnalysisRunConfiguration : IEntityTypeConfiguration<AnalysisRun>
{
    public void Configure(EntityTypeBuilder<AnalysisRun> builder)
    {
        builder.ToTable("analysis_runs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.GitHubOwner).HasColumnName("github_owner").HasMaxLength(255).IsRequired();
        builder.Property(x => x.GitHubRepo).HasColumnName("github_repo").HasMaxLength(255).IsRequired();
        builder.Property(x => x.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(x => x.EndDate).HasColumnName("end_date").IsRequired();
        builder.Property(x => x.BaseBranch).HasColumnName("base_branch").HasMaxLength(255).IsRequired();
        builder.Property(x => x.RunDate).HasColumnName("run_date").IsRequired();
        builder.Property(x => x.PrCount).HasColumnName("pr_count").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasMany(x => x.PullRequests)
            .WithOne(x => x.AnalysisRun)
            .HasForeignKey(x => x.AnalysisRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
