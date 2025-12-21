using Microsoft.EntityFrameworkCore;
using ProjectInsights.Data;
using ProjectInsights.Data.Entities;
using ProjectInsights.Repositories.Interfaces;

namespace ProjectInsights.Repositories.Implementations;

public class AnalysisRunRepository : IAnalysisRunRepository
{
    private readonly ProjectInsightsDbContext _context;

    public AnalysisRunRepository(ProjectInsightsDbContext context)
    {
        _context = context;
    }

    public async Task<AnalysisRun> CreateAsync(AnalysisRun analysisRun)
    {
        analysisRun.CreatedAt = DateTime.UtcNow;
        _context.AnalysisRuns.Add(analysisRun);
        await _context.SaveChangesAsync();
        return analysisRun;
    }

    public async Task<AnalysisRun?> GetLatestAsync(string owner, string repo)
    {
        return await _context.AnalysisRuns
            .Where(ar => ar.GitHubOwner == owner && ar.GitHubRepo == repo)
            .OrderByDescending(ar => ar.EndDate)
            .FirstOrDefaultAsync();
    }

    public async Task<AnalysisRun?> GetByIdAsync(long id)
    {
        return await _context.AnalysisRuns
            .Include(ar => ar.PullRequests)
            .FirstOrDefaultAsync(ar => ar.Id == id);
    }
}
