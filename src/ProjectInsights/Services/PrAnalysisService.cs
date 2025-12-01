using LibGit2Sharp;
using Octokit;
using ProjectInsights.Models;

namespace ProjectInsights.Services;

public class PrAnalysisService
{
    private readonly ProjectDiscoveryService _projectDiscovery;
    private readonly GitHubService _gitHubService;
    private readonly ConfigurationService _configService;
    private readonly string _repoPath;
    private readonly List<Models.Team> _teams;

    public PrAnalysisService(
        ProjectDiscoveryService projectDiscovery,
        GitHubService gitHubService,
        ConfigurationService configService,
        string repoPath,
        List<Models.Team> teams)
    {
        _projectDiscovery = projectDiscovery;
        _gitHubService = gitHubService;
        _configService = configService;
        _repoPath = repoPath;
        _teams = teams;
    }

    public async Task<List<PrInfo>> AnalyzePullRequestsAsync(DateTime startDate, DateTime endDate)
    {
        var prs = await _gitHubService.GetMergedPullRequestsAsync(startDate, endDate);
        var prInfoList = new List<PrInfo>();

        int count = 0;
        foreach (var pr in prs)
        {
            count++;
            Console.WriteLine($"Analyzing PR #{pr.Number}: {pr.Title} ({count}/{prs.Count})");

            var prInfo = new PrInfo
            {
                Number = pr.Number,
                Title = pr.Title,
                Author = pr.User.Login,
                MergedAt = pr.MergedAt!.Value.DateTime,
                Team = _configService.GetTeamForAuthor(_teams, pr.User.Login)
            };

            // Get files changed in this PR
            var files = await _gitHubService.GetPullRequestFilesAsync(pr.Number);

            // Group files by project group
            var filesByProjectGroup = new Dictionary<string, int>();
            
            foreach (var file in files)
            {
                var projectGroup = _projectDiscovery.GetProjectGroupForFile(file.FileName);
                
                if (!filesByProjectGroup.ContainsKey(projectGroup))
                {
                    filesByProjectGroup[projectGroup] = 0;
                }
                filesByProjectGroup[projectGroup]++;
            }

            prInfo.FileCountByProjectGroup = filesByProjectGroup;
            prInfoList.Add(prInfo);
        }

        return prInfoList;
    }

    public Dictionary<string, ProjectGroupStats> CalculateProjectGroupStats(List<PrInfo> prInfos)
    {
        Console.WriteLine("Calculating project group statistics...");
        
        var stats = new Dictionary<string, ProjectGroupStats>();
        var allProjectGroups = _projectDiscovery.GetAllProjectGroups();

        // Initialize stats for all known project groups
        foreach (var group in allProjectGroups)
        {
            stats[group] = new ProjectGroupStats
            {
                ProjectGroupName = group
            };
        }

        // Use LibGit2Sharp to get detailed file changes with whitespace ignore
        using var repo = new LibGit2Sharp.Repository(_repoPath);

        foreach (var prInfo in prInfos)
        {
            var prProjectGroups = new HashSet<string>();

            foreach (var kvp in prInfo.FileCountByProjectGroup)
            {
                var projectGroup = kvp.Key;
                prProjectGroups.Add(projectGroup);

                if (!stats.ContainsKey(projectGroup))
                {
                    stats[projectGroup] = new ProjectGroupStats
                    {
                        ProjectGroupName = projectGroup
                    };
                }
            }

            // Increment PR count for each project group touched by this PR
            foreach (var projectGroup in prProjectGroups)
            {
                stats[projectGroup].PrCount++;
            }
        }

        // For LOC and file stats, we need to analyze commits with LibGit2Sharp
        // This is a simplified version - in production, you'd need to map PRs to commits
        // For now, we'll use the GitHub API file stats
        foreach (var prInfo in prInfos)
        {
            foreach (var kvp in prInfo.FileCountByProjectGroup)
            {
                var projectGroup = kvp.Key;
                var fileCount = kvp.Value;

                if (stats.ContainsKey(projectGroup))
                {
                    stats[projectGroup].FilesModified += fileCount;
                    // Note: We'll need to enhance this with actual LOC and add/delete detection
                }
            }
        }

        return stats;
    }

    public async Task<Dictionary<string, ProjectGroupStats>> AnalyzeWithDetailedStatsAsync(
        List<PrInfo> prInfos, 
        List<PullRequest> pullRequests)
    {
        Console.WriteLine("Calculating detailed project group statistics with file analysis...");
        
        var stats = new Dictionary<string, ProjectGroupStats>();
        var allProjectGroups = _projectDiscovery.GetAllProjectGroups();

        // Initialize stats for all known project groups
        foreach (var group in allProjectGroups)
        {
            stats[group] = new ProjectGroupStats
            {
                ProjectGroupName = group
            };
        }

        // Track which PRs touched which project groups
        var prsByProjectGroup = new Dictionary<string, HashSet<int>>();

        foreach (var prInfo in prInfos)
        {
            var pr = pullRequests.First(p => p.Number == prInfo.Number);
            var files = await _gitHubService.GetPullRequestFilesAsync(pr.Number);

            foreach (var file in files)
            {
                var projectGroup = _projectDiscovery.GetProjectGroupForFile(file.FileName);

                if (!stats.ContainsKey(projectGroup))
                {
                    stats[projectGroup] = new ProjectGroupStats
                    {
                        ProjectGroupName = projectGroup
                    };
                }

                if (!prsByProjectGroup.ContainsKey(projectGroup))
                {
                    prsByProjectGroup[projectGroup] = new HashSet<int>();
                }
                prsByProjectGroup[projectGroup].Add(prInfo.Number);

                // Accumulate stats
                // Note: GitHub API provides changes which is additions + deletions
                // For whitespace-ignore logic, we'd need to use LibGit2Sharp directly on commits
                stats[projectGroup].TotalLinesChanged += file.Changes;

                if (file.Status == "added")
                {
                    stats[projectGroup].FilesAdded++;
                }
                else if (file.Status == "modified" || file.Status == "removed" || file.Status == "renamed")
                {
                    stats[projectGroup].FilesModified++;
                }
            }
        }

        // Set PR counts
        foreach (var kvp in prsByProjectGroup)
        {
            stats[kvp.Key].PrCount = kvp.Value.Count;
        }

        return stats;
    }
}
