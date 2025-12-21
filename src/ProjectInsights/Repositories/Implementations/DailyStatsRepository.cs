using Microsoft.EntityFrameworkCore;
using ProjectInsights.Data;
using ProjectInsights.Data.Entities;
using ProjectInsights.Repositories.Interfaces;

namespace ProjectInsights.Repositories.Implementations;

public class DailyStatsRepository : IDailyStatsRepository
{
    private readonly ProjectInsightsDbContext _context;

    public DailyStatsRepository(ProjectInsightsDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<(DateOnly day, string projectName), DailyProjectStats>> GetExistingDailyProjectStatsAsync(
        List<DateOnly> days, 
        List<string> projectNames)
    {
        var stats = await _context.DailyProjectStats
            .Where(s => days.Contains(s.Day) && projectNames.Contains(s.ProjectName))
            .ToListAsync();

        return stats.ToDictionary(s => (s.Day, s.ProjectName), s => s);
    }

    public async Task<Dictionary<(DateOnly day, string projectName, string teamName), DailyTeamProjectStats>> GetExistingDailyTeamProjectStatsAsync(
        List<DateOnly> days, 
        List<string> projectNames, 
        List<string> teamNames)
    {
        var stats = await _context.DailyTeamProjectStats
            .Where(s => days.Contains(s.Day) && 
                       projectNames.Contains(s.ProjectName) && 
                       teamNames.Contains(s.TeamName))
            .ToListAsync();

        return stats.ToDictionary(s => (s.Day, s.ProjectName, s.TeamName), s => s);
    }

    public async Task UpsertDailyProjectStatsAsync(List<DailyProjectStats> newStats)
    {
        if (!newStats.Any())
            return;

        // Get unique days and project names from new stats
        var days = newStats.Select(s => s.Day).Distinct().ToList();
        var projectNames = newStats.Select(s => s.ProjectName).Distinct().ToList();

        // Fetch existing records
        var existing = await GetExistingDailyProjectStatsAsync(days, projectNames);

        var now = DateTime.UtcNow;

        foreach (var newStat in newStats)
        {
            var key = (newStat.Day, newStat.ProjectName);
            
            if (existing.TryGetValue(key, out var existingRecord))
            {
                // Update existing record by adding new stats
                existingRecord.PrCount += newStat.PrCount;
                existingRecord.TotalLinesChanged += newStat.TotalLinesChanged;
                existingRecord.FilesModified += newStat.FilesModified;
                existingRecord.FilesAdded += newStat.FilesAdded;
                existingRecord.UpdatedAt = now;
                
                _context.DailyProjectStats.Update(existingRecord);
            }
            else
            {
                // Insert new record
                newStat.CreatedAt = now;
                newStat.UpdatedAt = now;
                _context.DailyProjectStats.Add(newStat);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpsertDailyTeamProjectStatsAsync(List<DailyTeamProjectStats> newStats)
    {
        if (!newStats.Any())
            return;

        // Get unique days, project names, and team names from new stats
        var days = newStats.Select(s => s.Day).Distinct().ToList();
        var projectNames = newStats.Select(s => s.ProjectName).Distinct().ToList();
        var teamNames = newStats.Select(s => s.TeamName).Distinct().ToList();

        // Fetch existing records
        var existing = await GetExistingDailyTeamProjectStatsAsync(days, projectNames, teamNames);

        var now = DateTime.UtcNow;

        foreach (var newStat in newStats)
        {
            var key = (newStat.Day, newStat.ProjectName, newStat.TeamName);
            
            if (existing.TryGetValue(key, out var existingRecord))
            {
                // Update existing record by adding new stats
                existingRecord.PrCount += newStat.PrCount;
                existingRecord.UpdatedAt = now;
                
                _context.DailyTeamProjectStats.Update(existingRecord);
            }
            else
            {
                // Insert new record
                newStat.CreatedAt = now;
                newStat.UpdatedAt = now;
                _context.DailyTeamProjectStats.Add(newStat);
            }
        }

        await _context.SaveChangesAsync();
    }
}
