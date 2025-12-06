using Octokit;

namespace ProjectInsights.Services;

public class GitHubService
{
    private readonly GitHubClient _client;
    private readonly string _owner;
    private readonly string _repo;
    private int? _rateLimitRemaining;
    private bool _firstRequest = true;

    public GitHubService(string token, string owner, string repo)
    {
        _owner = owner;
        _repo = repo;
        _client = new GitHubClient(new ProductHeaderValue("ProjectInsights"))
        {
            Credentials = new Credentials(token)
        };
    }

    public async Task<List<PullRequest>> GetMergedPullRequestsAsync(DateTime startDate, DateTime endDate)
    {
        Console.WriteLine($"Fetching PRs merged between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}...");

        // Use GitHub Search API with date filtering
        var searchRequest = new SearchIssuesRequest
        {
            Type = IssueTypeQualifier.PullRequest,
            Repos = new RepositoryCollection { $"{_owner}/{_repo}" },
            Is = new[] { IssueIsQualifier.Merged },
            Merged = new DateRange(startDate, endDate)
        };

        var searchResult = await _client.Search.SearchIssues(searchRequest);
        
        // Check rate limit on first request
        if (_firstRequest)
        {
            await CheckRateLimitAsync();
            _firstRequest = false;
        }

        Console.WriteLine($"Found {searchResult.TotalCount} merged PRs in date range");

        // Convert search results to PullRequest objects
        var prNumbers = searchResult.Items.Select(i => i.Number).ToList();
        var pullRequests = new List<PullRequest>();

        foreach (var prNumber in prNumbers)
        {
            var pr = await _client.PullRequest.Get(_owner, _repo, prNumber);
            pullRequests.Add(pr);
            
            // Check rate limit periodically
            if (pullRequests.Count % 50 == 0)
            {
                await CheckRateLimitAsync();
            }
        }
        
        // Final rate limit check
        await CheckRateLimitAsync();

        return pullRequests;
    }

    public async Task<List<PullRequestFile>> GetPullRequestFilesAsync(int prNumber)
    {
        var files = await _client.PullRequest.Files(_owner, _repo, prNumber);
        
        // Check rate limit after each request
        await CheckRateLimitAsync();

        return files.ToList();
    }

    public async Task CheckRateLimitAsync()
    {
        try
        {
            var rateLimit = await _client.RateLimit.GetRateLimits();
            var remaining = rateLimit.Resources.Core.Remaining;
            _rateLimitRemaining = remaining;

            Console.WriteLine($"GitHub API Rate Limit: {remaining} requests remaining");

            if (_firstRequest && remaining < 4000)
            {
                Console.WriteLine($"ERROR: Rate limit too low to start ({remaining} remaining). Need at least 4000.");
                Environment.Exit(2);
            }

            if (remaining <= 3000)
            {
                Console.WriteLine($"WARNING: Rate limit reached threshold ({remaining} remaining). Stopping execution.");
                Environment.Exit(2);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not check rate limit: {ex.Message}");
        }
    }

    public int? GetRateLimitRemaining() => _rateLimitRemaining;
}
