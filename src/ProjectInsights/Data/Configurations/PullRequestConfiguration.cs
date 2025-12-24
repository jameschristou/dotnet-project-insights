using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectInsights.Data.Entities;

namespace ProjectInsights.Data.Configurations;

public class PullRequestConfiguration : IEntityTypeConfiguration<PullRequest>
{
    public void Configure(EntityTypeBuilder<PullRequest> builder)
    {
        builder.ToTable("pull_requests");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AnalysisRunId).HasColumnName("analysis_run_id").IsRequired();
        builder.Property(x => x.PrNumber).HasColumnName("pr_number").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").IsRequired();
        builder.Property(x => x.Author).HasColumnName("author").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Team).HasColumnName("team").HasMaxLength(255).IsRequired();
        builder.Property(x => x.MergedAt).HasColumnName("merged_at").IsRequired();
        builder.Property(x => x.MergeCommitSha).HasColumnName("merge_commit_sha").HasMaxLength(40).IsRequired();
        builder.Property(x => x.IsRollupPr).HasColumnName("is_rollup_pr").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        // Unique constraint on pr_number to ensure each PR is only processed once
        builder.HasIndex(x => x.PrNumber).IsUnique();

        builder.HasMany(x => x.PrFiles)
            .WithOne(x => x.PullRequest)
            .HasForeignKey(x => x.PullRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.PrProjects)
            .WithOne(x => x.PullRequest)
            .HasForeignKey(x => x.PullRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
