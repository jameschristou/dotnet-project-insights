using ProjectInsights.Models;

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
    /// Determines if a PR should be processed (i.e., is not a rollup PR or revert PR).
    /// Rollup PRs are already expanded into individual PRs by GitHubService.
    /// Revert PRs are skipped as they undo previous changes.
    /// </summary>
    /// <param name="pr">The pull request to check.</param>
    /// <returns>True if the PR should be processed, false if it should be ignored.</returns>
    public bool ShouldProcessPr(LocalPullRequest pr)
    {
        // Skip rollup PRs (we process the individual PRs instead)
        if (pr.IsRollupPr)
            return false;

        // Skip revert PRs
        if (!string.IsNullOrEmpty(pr.Title) && pr.Title.Contains("revert", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    public async Task<List<PrInfo>> AnalyzePullRequestsAsync(DateTime dayMerged, string baseBranch)
    {
        // Get PRs from GitHub API (includes rollup expansion)
        var prs = await _gitHubService.GetMergedPullRequestsAsync(dayMerged, baseBranch);
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
                // Use head/base comparison for accurate file tracking
                // This compares the PR's branch tip against its base, showing only the PR's changes
                if (!string.IsNullOrEmpty(pr.HeadSha) && !string.IsNullOrEmpty(pr.BaseSha))
                {
                    files = _gitService.GetPullRequestFilesByHeadAndBase(pr.HeadSha, pr.BaseSha);
                }
                else
                {
                    // Fallback to merge commit analysis if head/base not available
                    files = _gitService.GetPullRequestFiles(pr.MergeCommitSha);
                }
                
                // Calculate project information for each file
                foreach (var file in files)
                {
                    file.ProjectName = _projectDiscovery.GetProjectNameForFile(file.FileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Warning: Could not analyze files for PR #{pr.Number}: {ex.Message}");
                files = new List<LocalPullRequestFile>();
            }

            // Group files by project name
            var filesByProjectName = new Dictionary<string, int>();
            foreach (var file in files)
            {
                if (!filesByProjectName.ContainsKey(file.ProjectName))
                {
                    filesByProjectName[file.ProjectName] = 0;
                }
                filesByProjectName[file.ProjectName]++;
            }

            var prInfo = new PrInfo
            {
                Number = pr.Number,
                Title = pr.Title,
                Author = pr.Author,
                MergedAt = pr.MergedAt,
                Team = _configService.GetTeamForAuthor(_teams, pr.Author),
                FileCountByProjectName = filesByProjectName,
                Files = files
            };

            prInfoList.Add(prInfo);
        }

        return prInfoList;
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
                var projectName = file.ProjectName;

                if (!stats.ContainsKey(projectName))
                {
                    stats[projectName] = new ProjectGroupStats
                    {
                        ProjectGroupName = projectName
                    };
                }

                if (!prsByProjectGroup.ContainsKey(projectName))
                {
                    prsByProjectGroup[projectName] = new HashSet<int>();
                }
                prsByProjectGroup[projectName].Add(prInfo.Number);

                // Accumulate stats
                stats[projectName].TotalLinesChanged += file.Changes;

                if (file.Status == "added")
                {
                    stats[projectName].FilesAdded++;
                }
                else if (file.Status == "modified" || file.Status == "renamed")
                {
                    stats[projectName].FilesModified++;
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
