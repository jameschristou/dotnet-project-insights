using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectInsights.Data.Entities;

namespace ProjectInsights.Data.Configurations;

public class DailyTeamProjectStatsConfiguration : IEntityTypeConfiguration<DailyTeamProjectStats>
{
    public void Configure(EntityTypeBuilder<DailyTeamProjectStats> builder)
    {
        builder.ToTable("daily_team_project_stats");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Day).HasColumnName("day").IsRequired();
        builder.Property(x => x.ProjectName).HasColumnName("project_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.ProjectGroup).HasColumnName("project_group").HasMaxLength(255).IsRequired();
        builder.Property(x => x.TeamName).HasColumnName("team_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.PrCount).HasColumnName("pr_count").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(x => new { x.Day, x.ProjectName, x.TeamName }).IsUnique();
        builder.HasIndex(x => x.Day);
        builder.HasIndex(x => x.ProjectGroup);
        builder.HasIndex(x => x.TeamName);
    }
}
