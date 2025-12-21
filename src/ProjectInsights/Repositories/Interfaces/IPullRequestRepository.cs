using ProjectInsights.Data.Entities;

namespace ProjectInsights.Repositories.Interfaces;

public interface IPullRequestRepository
{
    Task BulkInsertPullRequestsAsync(List<PullRequest> pullRequests);
    Task BulkInsertPrFilesAsync(List<PrFile> prFiles);
    Task BulkInsertPrProjectsAsync(List<PrProject> prProjects);
    Task<List<PullRequest>> GetByAnalysisRunIdAsync(long analysisRunId);
}
