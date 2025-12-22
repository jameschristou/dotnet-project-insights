using ProjectInsights.Data.Entities;
using ProjectInsights.Models;
using ProjectInsights.Repositories.Interfaces;

namespace ProjectInsights.Services;

public class DatabaseExportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ProjectDiscoveryService _projectDiscovery;

    public DatabaseExportService(IUnitOfWork unitOfWork, ProjectDiscoveryService projectDiscovery)
    {
        _unitOfWork = unitOfWork;
        _projectDiscovery = projectDiscovery;
    }

    public async Task ExportDataAsync(
        string owner,
        string repo,
        DateTime startDate,
        DateTime endDate,
        string baseBranch,
        List<PrInfo> prInfos)
    {
        Console.WriteLine("Exporting data to PostgreSQL database...");

        // Transaction is managed by caller
        // 1. Create analysis run record
        var analysisRun = await CreateAnalysisRunAsync(owner, repo, startDate, endDate, baseBranch, prInfos.Count);
        Console.WriteLine($"Created analysis run with ID: {analysisRun.Id}");

        // 2. Save pull requests, files, and projects
        await SavePullRequestsAsync(analysisRun.Id, prInfos);
        Console.WriteLine($"Saved {prInfos.Count} pull requests with files and projects");

        // 3. Calculate and upsert daily stats
        await CalculateAndUpsertDailyStatsAsync(prInfos);
        Console.WriteLine("Updated daily project and team statistics");

        Console.WriteLine("Data export completed successfully");
    }

    private async Task<AnalysisRun> CreateAnalysisRunAsync(
        string owner, 
        string repo, 
        DateTime startDate, 
        DateTime endDate, 
        string baseBranch, 
        int prCount)
    {
        var analysisRun = new AnalysisRun
        {
            GitHubOwner = owner,
            GitHubRepo = repo,
            StartDate = startDate,
            EndDate = endDate,
            BaseBranch = baseBranch,
            RunDate = DateTime.UtcNow,
            PrCount = prCount
        };

        return await _unitOfWork.AnalysisRuns.CreateAsync(analysisRun);
    }

    private async Task SavePullRequestsAsync(long analysisRunId, List<PrInfo> prInfos)
    {
        var pullRequests = new List<PullRequest>();
        var allPrFiles = new List<PrFile>();
        var allPrProjects = new List<PrProject>();

        foreach (var prInfo in prInfos)
        {
            var pullRequest = new PullRequest
            {
                AnalysisRunId = analysisRunId,
                PrNumber = prInfo.Number,
                Title = prInfo.Title,
                Author = prInfo.Author,
                Team = prInfo.Team,
                MergedAt = prInfo.MergedAt,
                MergeCommitSha = string.Empty, // Not available in PrInfo
                IsRollupPr = false
            };

            pullRequests.Add(pullRequest);
        }

        // Insert PRs first to get their IDs
        await _unitOfWork.PullRequests.BulkInsertPullRequestsAsync(pullRequests);

        // Now build files and projects with PR IDs
        for (int i = 0; i < prInfos.Count; i++)
        {
            var prInfo = prInfos[i];
            var pullRequest = pullRequests[i];

            // Add files - use pre-calculated project information
            foreach (var file in prInfo.Files)
            {
                var prFile = new PrFile
                {
                    PullRequestId = pullRequest.Id,
                    FileName = file.FileName,
                    ProjectName = file.ProjectName,
                    ProjectGroup = _projectDiscovery.GetProjectGroup(file.ProjectName),
                    Status = file.Status,
                    Additions = file.Additions,
                    Deletions = file.Deletions,
                    Changes = file.Changes
                };

                allPrFiles.Add(prFile);
            }

            // Add projects (aggregated from files)
            foreach (var kvp in prInfo.FileCountByProjectName)
            {
                var projectName = kvp.Key;
                var fileCount = kvp.Value;

                var prProject = new PrProject
                {
                    PullRequestId = pullRequest.Id,
                    ProjectName = projectName,
                    ProjectGroup = _projectDiscovery.GetProjectGroup(projectName),
                    FileCount = fileCount
                };

                allPrProjects.Add(prProject);
            }
        }

        await _unitOfWork.PullRequests.BulkInsertPrFilesAsync(allPrFiles);
        await _unitOfWork.PullRequests.BulkInsertPrProjectsAsync(allPrProjects);
    }

    private async Task CalculateAndUpsertDailyStatsAsync(List<PrInfo> prInfos)
    {
        // Group PRs by day and project
        var dailyProjectStats = new Dictionary<(DateOnly day, string projectName), DailyProjectStats>();
        var dailyTeamProjectStats = new Dictionary<(DateOnly day, string projectName, string teamName), DailyTeamProjectStats>();

        foreach (var prInfo in prInfos)
        {
            var day = DateOnly.FromDateTime(prInfo.MergedAt);

            foreach (var file in prInfo.Files)
            {
                var projectName = file.ProjectName;

                // Update daily project stats
                var projectKey = (day, projectName);
                if (!dailyProjectStats.TryGetValue(projectKey, out var projectStat))
                {
                    projectStat = new DailyProjectStats
                    {
                        Day = day,
                        ProjectName = projectName,
                        ProjectGroup = _projectDiscovery.GetProjectGroup(projectName),
                        PrCount = 0,
                        TotalLinesChanged = 0,
                        FilesModified = 0,
                        FilesAdded = 0
                    };
                    dailyProjectStats[projectKey] = projectStat;
                }

                // Only count PR once per project per day
                if (file == prInfo.Files.First(f => f.ProjectName == projectName))
                {
                    projectStat.PrCount++;
                }

                projectStat.TotalLinesChanged += file.Changes;

                if (file.Status == "added")
                {
                    projectStat.FilesAdded++;
                }
                else if (file.Status == "modified" || file.Status == "removed" || file.Status == "renamed")
                {
                    projectStat.FilesModified++;
                }

                // Update daily team project stats
                var teamProjectKey = (day, projectName, prInfo.Team);
                if (!dailyTeamProjectStats.TryGetValue(teamProjectKey, out var teamProjectStat))
                {
                    teamProjectStat = new DailyTeamProjectStats
                    {
                        Day = day,
                        ProjectName = projectName,
                        ProjectGroup = _projectDiscovery.GetProjectGroup(projectName),
                        TeamName = prInfo.Team,
                        PrCount = 0
                    };
                    dailyTeamProjectStats[teamProjectKey] = teamProjectStat;
                }

                // Only count PR once per project per team per day
                if (file == prInfo.Files.First(f => f.ProjectName == projectName))
                {
                    teamProjectStat.PrCount++;
                }
            }
        }

        // Upsert stats
        await _unitOfWork.DailyStats.UpsertDailyProjectStatsAsync(dailyProjectStats.Values.ToList());
        await _unitOfWork.DailyStats.UpsertDailyTeamProjectStatsAsync(dailyTeamProjectStats.Values.ToList());
    }
}
