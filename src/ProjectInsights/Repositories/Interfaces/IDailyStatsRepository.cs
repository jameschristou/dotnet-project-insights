using ProjectInsights.Data.Entities;

namespace ProjectInsights.Repositories.Interfaces;

public interface IDailyStatsRepository
{
    Task<Dictionary<(DateOnly day, string projectName), DailyProjectStats>> GetExistingDailyProjectStatsAsync(
        List<DateOnly> days, 
        List<string> projectNames);
    
    Task<Dictionary<(DateOnly day, string projectName, string teamName), DailyTeamProjectStats>> GetExistingDailyTeamProjectStatsAsync(
        List<DateOnly> days, 
        List<string> projectNames, 
        List<string> teamNames);
    
    Task UpsertDailyProjectStatsAsync(List<DailyProjectStats> stats);
    
    Task UpsertDailyTeamProjectStatsAsync(List<DailyTeamProjectStats> stats);
}
