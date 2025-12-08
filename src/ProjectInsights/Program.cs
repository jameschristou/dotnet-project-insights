using ProjectInsights.Services;

namespace ProjectInsights;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var config = ParseArguments(args);
            await RunAnalysisAsync(config);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static Models.AppConfig ParseArguments(string[] args)
    {
        var config = new Models.AppConfig();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--") && i + 1 < args.Length)
            {
                var key = args[i].ToLower();
                var value = args[i + 1];

                switch (key)
                {
                    case "--start-date":
                        config.StartDate = DateTime.Parse(value);
                        break;
                    case "--end-date":
                        config.EndDate = DateTime.Parse(value);
                        break;
                    case "--project-groups":
                        config.ProjectGroupsPath = value;
                        break;
                    case "--teams":
                        config.TeamsPath = value;
                        break;
                    case "--github-pat":
                        config.GitHubPat = value;
                        break;
                    case "--google-creds":
                        config.GoogleCredsPath = value;
                        break;
                    case "--spreadsheet-id":
                        config.SpreadsheetId = value;
                        break;
                    case "--repo-path":
                        config.RepoPath = value;
                        break;
                    case "--github-owner":
                        config.GitHubOwner = value;
                        break;
                    case "--github-repo":
                        config.GitHubRepo = value;
                        break;
                    case "--github-base-branch":
                        config.GitHubBaseBranch = value;
                        break;
                }
                i++; // Skip the value in next iteration
            }
        }

        // Validate required fields
        if (config.StartDate == default || config.EndDate == default)
            throw new ArgumentException("--start-date and --end-date are required");
        if (string.IsNullOrEmpty(config.ProjectGroupsPath))
            throw new ArgumentException("--project-groups is required");
        if (string.IsNullOrEmpty(config.TeamsPath))
            throw new ArgumentException("--teams is required");
        if (string.IsNullOrEmpty(config.GitHubPat))
            throw new ArgumentException("--github-pat is required");
        if (string.IsNullOrEmpty(config.GoogleCredsPath))
            throw new ArgumentException("--google-creds is required");
        if (string.IsNullOrEmpty(config.SpreadsheetId))
            throw new ArgumentException("--spreadsheet-id is required");
        if (string.IsNullOrEmpty(config.RepoPath))
            throw new ArgumentException("--repo-path is required");
        if (string.IsNullOrEmpty(config.GitHubOwner))
            throw new ArgumentException("--github-owner is required");
        if (string.IsNullOrEmpty(config.GitHubRepo))
            throw new ArgumentException("--github-repo is required");

        return config;
    }

    static async Task RunAnalysisAsync(Models.AppConfig config)
    {
        Console.WriteLine("=== ProjectInsights ===");
        Console.WriteLine($"Date Range: {config.StartDate:yyyy-MM-dd} to {config.EndDate:yyyy-MM-dd}");
        Console.WriteLine($"Repository: {config.GitHubOwner}/{config.GitHubRepo}");
        Console.WriteLine($"Local Path: {config.RepoPath}");
        Console.WriteLine();

        // 1. Load configuration
        Console.WriteLine("Loading configuration...");
        var configService = new ConfigurationService();
        var projectGroups = configService.LoadProjectGroups(config.ProjectGroupsPath);
        var teams = configService.LoadTeams(config.TeamsPath);
        Console.WriteLine($"Loaded {projectGroups.Count} project groups and {teams.Count} teams");
        Console.WriteLine();

        // 2. Discover projects
        var projectDiscovery = new ProjectDiscoveryService(config.RepoPath, projectGroups);
        projectDiscovery.DiscoverProjects();
        var allProjectGroups = projectDiscovery.GetAllProjectGroups();
        Console.WriteLine();

        // 3. Initialize GitHub service
        var githubService = new GitHubService(config.GitHubPat, config.GitHubOwner, config.GitHubRepo);
        Console.WriteLine();

        // 4. Analyze PRs
        var prAnalysisService = new PrAnalysisService(
            projectDiscovery,
            githubService,
            configService,
            config.RepoPath,
            teams,
            config.GitHubOwner,
            config.GitHubRepo);

        var prBatchProcessor = new PrBatchProcessor(prAnalysisService, githubService);

        var prInfos = await prBatchProcessor.ProcessPrsInBatchesAsync(config.StartDate, config.EndDate, config.GitHubBaseBranch);
        Console.WriteLine();

        // 5. Get detailed stats
        var projectGroupStats = await prAnalysisService.AnalyzeWithDetailedStatsAsync(prInfos);
        Console.WriteLine();

        // 6. Build teams matrix
        var dataAggregation = new DataAggregationService();
        var teamsMatrix = dataAggregation.BuildTeamsMatrix(prInfos, allProjectGroups, teams);
        var allTeams = dataAggregation.GetAllTeamNames(teams);
        Console.WriteLine();

        // 7. Export to Google Sheets
        var googleSheetsService = new GoogleSheetsService(config.GoogleCredsPath, config.SpreadsheetId);
        await googleSheetsService.ExportDataAsync(
            projectGroupStats,
            prInfos,
            teamsMatrix,
            allProjectGroups,
            allTeams);

        Console.WriteLine();
        Console.WriteLine("=== Analysis Complete ===");
        Console.WriteLine($"Analyzed {prInfos.Count} PRs across {allProjectGroups.Count} project groups");
    }
}
