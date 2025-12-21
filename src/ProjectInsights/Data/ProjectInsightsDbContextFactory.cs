using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProjectInsights.Data;

public class ProjectInsightsDbContextFactory : IDesignTimeDbContextFactory<ProjectInsightsDbContext>
{
    public ProjectInsightsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProjectInsightsDbContext>();
        
        // Default connection string for migrations
        // Override with actual connection string at runtime
        var connectionString = "Host=localhost;Port=5432;Database=projectinsights;Username=postgres;Password=";
        
        optionsBuilder.UseNpgsql(connectionString);

        return new ProjectInsightsDbContext(optionsBuilder.Options);
    }
}
