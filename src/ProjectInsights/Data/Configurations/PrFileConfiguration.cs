using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectInsights.Data.Entities;

namespace ProjectInsights.Data.Configurations;

public class PrFileConfiguration : IEntityTypeConfiguration<PrFile>
{
    public void Configure(EntityTypeBuilder<PrFile> builder)
    {
        builder.ToTable("pr_files");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.PullRequestId).HasColumnName("pull_request_id").IsRequired();
        builder.Property(x => x.FileName).HasColumnName("file_name").IsRequired();
        builder.Property(x => x.ProjectName).HasColumnName("project_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.ProjectGroup).HasColumnName("project_group").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Additions).HasColumnName("additions").IsRequired();
        builder.Property(x => x.Deletions).HasColumnName("deletions").IsRequired();
        builder.Property(x => x.Changes).HasColumnName("changes").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.PullRequestId);
        builder.HasIndex(x => x.ProjectName);
        builder.HasIndex(x => x.ProjectGroup);
    }
}
