using ProjectInsights.Models;
using System.Text.RegularExpressions;

namespace ProjectInsights.Services;

public class PrAnalysisService
{
    private readonly ProjectDiscoveryService _projectDiscovery;
    private readonly GitHubService _gitHubService;
    private readonly GitService _gitService;
    private readonly ConfigurationService _configService;
    private readonly string _repoPath;
    private readonly List<Models.Team> _teams;

    public PrAnalysisService(
        ProjectDiscoveryService projectDiscovery,
        GitHubService gitHubService,
        GitService gitService,
        ConfigurationService configService,
        string repoPath,
        List<Models.Team> teams)
    {
        _projectDiscovery = projectDiscovery;
        _gitHubService = gitHubService;
        _gitService = gitService;
        _configService = configService;
        _repoPath = repoPath;
        _teams = teams;
    }

    /// <summary>
    /// Determines if a PR should be processed (i.e., is not a rollup PR).
    /// Rollup PRs are already expanded into individual PRs by GitHubService.
    /// </summary>
    /// <param name="pr">The pull request to check.</param>
    /// <returns>True if the PR should be processed, false if it should be ignored.</returns>
    public bool ShouldProcessPr(LocalPullRequest pr)
    {
        // Skip rollup PRs (we process the individual PRs instead)
        if (pr.IsRollupPr)
            return false;

        return true;
    }

    public async Task<List<PrInfo>> AnalyzePullRequestsAsync(DateTime startDate, DateTime endDate, string baseBranch)
    {
        // Get PRs from GitHub API (includes rollup expansion)
        var prs = await _gitHubService.GetMergedPullRequestsAsync(startDate, endDate, baseBranch);
        var prInfoList = new List<PrInfo>();

        int count = 0;
        foreach (var pr in prs)
        {
            if (!ShouldProcessPr(pr))
            {
                Console.WriteLine($"Skipping rollup PR #{pr.Number}: {pr.Title}");
                continue;
            }

            count++;
            Console.WriteLine($"Analyzing PR #{pr.Number}: {pr.Title} ({count}/{prs.Count})");

            // Get files changed in this PR using LibGit2Sharp
            List<LocalPullRequestFile> files;
            try
            {
                files = _gitService.GetPullRequestFiles(pr.MergeCommitSha);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Warning: Could not analyze files for PR #{pr.Number}: {ex.Message}");
                files = new List<LocalPullRequestFile>();
            }

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

            var prInfo = new PrInfo
            {
                Number = pr.Number,
                Title = pr.Title,
                Author = pr.Author,
                MergedAt = pr.MergedAt,
                Team = _configService.GetTeamForAuthor(_teams, pr.Author),
                FileCountByProjectGroup = filesByProjectGroup,
                Files = files
            };

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

        // For LOC and file stats, we use the file stats from our Git analysis
        foreach (var prInfo in prInfos)
        {
            foreach (var kvp in prInfo.FileCountByProjectGroup)
            {
                var projectGroup = kvp.Key;
                var fileCount = kvp.Value;

                if (stats.ContainsKey(projectGroup))
                {
                    stats[projectGroup].FilesModified += fileCount;
                }
            }
        }

        return stats;
    }

    public Task<Dictionary<string, ProjectGroupStats>> AnalyzeWithDetailedStatsAsync(List<PrInfo> prInfos)
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
            // Use cached file details from PrInfo
            var files = prInfo.Files;

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

        return Task.FromResult(stats);
    }
}
