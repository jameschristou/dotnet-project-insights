using Microsoft.EntityFrameworkCore;
using ProjectInsights.Data;
using ProjectInsights.Data.Entities;
using ProjectInsights.Repositories.Interfaces;

namespace ProjectInsights.Repositories.Implementations;

public class PullRequestRepository : IPullRequestRepository
{
    private readonly ProjectInsightsDbContext _context;

    public PullRequestRepository(ProjectInsightsDbContext context)
    {
        _context = context;
    }

    public async Task BulkInsertPullRequestsAsync(List<PullRequest> pullRequests)
    {
        foreach (var pr in pullRequests)
        {
            pr.CreatedAt = DateTime.UtcNow;
        }
        
        _context.PullRequests.AddRange(pullRequests);
        await _context.SaveChangesAsync();
    }

    public async Task BulkInsertPrFilesAsync(List<PrFile> prFiles)
    {
        foreach (var file in prFiles)
        {
            file.CreatedAt = DateTime.UtcNow;
        }
        
        _context.PrFiles.AddRange(prFiles);
        await _context.SaveChangesAsync();
    }

    public async Task BulkInsertPrProjectsAsync(List<PrProject> prProjects)
    {
        foreach (var project in prProjects)
        {
            project.CreatedAt = DateTime.UtcNow;
        }
        
        _context.PrProjects.AddRange(prProjects);
        await _context.SaveChangesAsync();
    }

    public async Task<List<PullRequest>> GetByAnalysisRunIdAsync(long analysisRunId)
    {
        return await _context.PullRequests
            .Include(pr => pr.PrFiles)
            .Include(pr => pr.PrProjects)
            .Where(pr => pr.AnalysisRunId == analysisRunId)
            .ToListAsync();
    }
}
