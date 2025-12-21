using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectInsights.Data.Entities;

namespace ProjectInsights.Data.Configurations;

public class PrProjectConfiguration : IEntityTypeConfiguration<PrProject>
{
    public void Configure(EntityTypeBuilder<PrProject> builder)
    {
        builder.ToTable("pr_projects");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.PullRequestId).HasColumnName("pull_request_id").IsRequired();
        builder.Property(x => x.ProjectName).HasColumnName("project_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.ProjectGroup).HasColumnName("project_group").HasMaxLength(255).IsRequired();
        builder.Property(x => x.FileCount).HasColumnName("file_count").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(x => new { x.PullRequestId, x.ProjectName }).IsUnique();
    }
}
