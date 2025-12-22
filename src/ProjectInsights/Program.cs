using Microsoft.EntityFrameworkCore;
using ProjectInsights.Data;
using ProjectInsights.Repositories.Implementations;
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
                        config.StartDate = DateTime.SpecifyKind(DateTime.Parse(value), DateTimeKind.Utc);
                        break;
                    case "--end-date":
                        config.EndDate = DateTime.SpecifyKind(DateTime.Parse(value), DateTimeKind.Utc);
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
                    case "--postgres-connection":
                        config.PostgresConnectionString = value;
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

        // Validate export destination (either Google Sheets OR PostgreSQL)
        var hasGoogleSheets = !string.IsNullOrEmpty(config.GoogleCredsPath) && !string.IsNullOrEmpty(config.SpreadsheetId);
        var hasPostgres = !string.IsNullOrEmpty(config.PostgresConnectionString);

        if (!hasGoogleSheets && !hasPostgres)
            throw new ArgumentException("Either (--google-creds AND --spreadsheet-id) OR --postgres-connection is required");

        // For Google Sheets, dates are required
        if (hasGoogleSheets && (config.StartDate == default || config.EndDate == default))
            throw new ArgumentException("--start-date and --end-date are required for Google Sheets export");

        // Validate other required fields
        if (string.IsNullOrEmpty(config.ProjectGroupsPath))
            throw new ArgumentException("--project-groups is required");
        if (string.IsNullOrEmpty(config.TeamsPath))
            throw new ArgumentException("--teams is required");
        if (string.IsNullOrEmpty(config.GitHubPat))
            throw new ArgumentException("--github-pat is required");
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
        // For PostgreSQL mode, determine date range from database
        if (!string.IsNullOrEmpty(config.PostgresConnectionString))
        {
            await DeterminePostgreSqlDateRangeAsync(config);
        }

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

        // 3. Initialize GitHub service (for PR discovery)
        var githubService = new GitHubService(config.GitHubPat, config.GitHubOwner, config.GitHubRepo);
        
        // 4. Initialize Git service (for file analysis)
        var gitService = new GitService(config.RepoPath);
        Console.WriteLine();

        // 5. Analyze PRs
        var prAnalysisService = new PrAnalysisService(
            projectDiscovery,
            githubService,
            gitService,
            configService,
            config.RepoPath,
            teams);

        var prBatchProcessor = new PrBatchProcessor(prAnalysisService, githubService);

        var prInfos = await prBatchProcessor.ProcessPrsInBatchesAsync(config.StartDate, config.EndDate, config.GitHubBaseBranch);
        Console.WriteLine();

        // 6. Export data - choose destination based on configuration
        if (!string.IsNullOrEmpty(config.PostgresConnectionString))
        {
            Console.WriteLine("Export destination: PostgreSQL Database");
            await ExportToPostgreSqlAsync(config, projectDiscovery, prInfos);
        }
        else
        {
            Console.WriteLine("Export destination: Google Sheets");
            await ExportToGoogleSheetsAsync(config, prAnalysisService, prInfos, allProjectGroups, teams);
        }

        Console.WriteLine();
        Console.WriteLine("=== Analysis Complete ===");
        Console.WriteLine($"Analyzed {prInfos.Count} PRs across {allProjectGroups.Count} project groups");
    }

    static async Task DeterminePostgreSqlDateRangeAsync(Models.AppConfig config)
    {
        // Create DbContext to query for the last analysis run
        var optionsBuilder = new DbContextOptionsBuilder<ProjectInsightsDbContext>();
        optionsBuilder.UseNpgsql(config.PostgresConnectionString);
        
        using var dbContext = new ProjectInsightsDbContext(optionsBuilder.Options);
        using var unitOfWork = new UnitOfWork(dbContext);

        Console.WriteLine("Determining date range from previous analysis runs...");
        
        var lastRun = await unitOfWork.AnalysisRuns.GetLatestAsync(config.GitHubOwner, config.GitHubRepo);
        
        if (lastRun != null)
        {
            // Use the last run's end date as the new start date
            config.StartDate = DateTime.SpecifyKind(lastRun.EndDate, DateTimeKind.Utc);
            config.EndDate = config.StartDate.AddDays(1);
            
            Console.WriteLine($"Found previous run ending on {lastRun.EndDate:yyyy-MM-dd}");
            Console.WriteLine($"Continuing from {config.StartDate:yyyy-MM-dd} to {config.EndDate:yyyy-MM-dd}");
        }
        else
        {
            // First run - use default date
            config.StartDate = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            config.EndDate = config.StartDate.AddDays(1);
            
            Console.WriteLine("No previous runs found - starting from default date 2025-11-01");
        }
        
        Console.WriteLine();
    }

    static async Task ExportToPostgreSqlAsync(
        Models.AppConfig config, 
        ProjectDiscoveryService projectDiscovery,
        List<Models.PrInfo> prInfos)
    {
        // Create DbContext
        var optionsBuilder = new DbContextOptionsBuilder<ProjectInsightsDbContext>();
        optionsBuilder.UseNpgsql(config.PostgresConnectionString);
        
        using var dbContext = new ProjectInsightsDbContext(optionsBuilder.Options);
        
        // Create UnitOfWork
        using var unitOfWork = new UnitOfWork(dbContext);
        
        try
        {
            // Start transaction for entire operation
            await unitOfWork.BeginTransactionAsync();
            
            // Create DatabaseExportService
            var dbExportService = new DatabaseExportService(unitOfWork, projectDiscovery);
            
            // Export data (this will use the existing transaction)
            await dbExportService.ExportDataAsync(
                config.GitHubOwner,
                config.GitHubRepo,
                config.StartDate,
                config.EndDate,
                config.GitHubBaseBranch,
                prInfos);
            
            // Commit transaction - data only persists if everything succeeds
            await unitOfWork.CommitTransactionAsync();
            Console.WriteLine("All data committed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during database export: {ex.Message}");
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    static async Task ExportToGoogleSheetsAsync(
        Models.AppConfig config,
        PrAnalysisService prAnalysisService,
        List<Models.PrInfo> prInfos,
        List<string> allProjectGroups,
        List<Models.Team> teams)
    {
        // Get detailed stats
        //var projectGroupStats = await prAnalysisService.AnalyzeWithDetailedStatsAsync(prInfos);
        //Console.WriteLine();

        // Build teams matrix
        var dataAggregation = new DataAggregationService();
        var teamsMatrix = dataAggregation.BuildTeamsMatrix(prInfos, allProjectGroups, teams);
        var allTeams = dataAggregation.GetAllTeamNames(teams);
        Console.WriteLine();

        //// Export to Google Sheets
        //var googleSheetsService = new GoogleSheetsService(config.GoogleCredsPath, config.SpreadsheetId);
        //await googleSheetsService.ExportDataAsync(
        //    projectGroupStats,
        //    prInfos,
        //    teamsMatrix,
        //    allProjectGroups,
        //    allTeams);
    }
}
