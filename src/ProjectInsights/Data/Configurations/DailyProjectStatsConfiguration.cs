using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectInsights.Data.Entities;

namespace ProjectInsights.Data.Configurations;

public class DailyProjectStatsConfiguration : IEntityTypeConfiguration<DailyProjectStats>
{
    public void Configure(EntityTypeBuilder<DailyProjectStats> builder)
    {
        builder.ToTable("daily_project_stats");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Day).HasColumnName("day").IsRequired();
        builder.Property(x => x.ProjectName).HasColumnName("project_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.ProjectGroup).HasColumnName("project_group").HasMaxLength(255).IsRequired();
        builder.Property(x => x.PrCount).HasColumnName("pr_count").IsRequired();
        builder.Property(x => x.TotalLinesChanged).HasColumnName("total_lines_changed").IsRequired();
        builder.Property(x => x.FilesModified).HasColumnName("files_modified").IsRequired();
        builder.Property(x => x.FilesAdded).HasColumnName("files_added").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(x => new { x.Day, x.ProjectName }).IsUnique();
        builder.HasIndex(x => x.Day);
        builder.HasIndex(x => x.ProjectGroup);
    }
}
