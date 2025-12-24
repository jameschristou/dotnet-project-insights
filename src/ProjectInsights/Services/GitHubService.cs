using Octokit;
using ProjectInsights.Models;

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

    /// <summary>
    /// Gets merged PRs using GitHub API, including detecting and expanding rollup PRs.
    /// </summary>
    public async Task<List<LocalPullRequest>> GetMergedPullRequestsAsync(DateTime dayMerged, string baseBranch)
    {
        Console.WriteLine($"Fetching PRs merged on {dayMerged:yyyy-MM-dd} into branch '{baseBranch}'...");

        // Use GitHub Search API with date filtering and base branch
        var searchRequest = new SearchIssuesRequest
        {
            Type = IssueTypeQualifier.PullRequest,
            Repos = new RepositoryCollection { $"{_owner}/{_repo}" },
            Is = new[] { IssueIsQualifier.Merged },
            Merged = new DateRange(dayMerged, dayMerged),
            Base = baseBranch
        };

        var searchResult = await _client.Search.SearchIssues(searchRequest);
        
        // Check rate limit on first request
        if (_firstRequest)
        {
            await CheckRateLimitAsync();
            _firstRequest = false;
        }

        Console.WriteLine($"Found {searchResult.TotalCount} merged PRs in date range and branch");

        var pullRequestsDict = new Dictionary<int, LocalPullRequest>();

        // Convert search results to PullRequest objects
        var prNumbers = searchResult.Items.Select(i => i.Number).Distinct().ToList();

        foreach (var prNumber in prNumbers)
        {
            Console.WriteLine($"Getting PR #{prNumber} using API");

            var pr = await _client.PullRequest.Get(_owner, _repo, prNumber);
            
            // Check if this is a rollup PR (title contains "release" or body has 2+ PR links)
            var isRollupPr = IsRollupPr(pr);

            var localPr = new LocalPullRequest
            {
                Number = pr.Number,
                Title = pr.Title,
                Author = pr.User.Login,
                MergedAt = DateTime.SpecifyKind(pr.MergedAt!.Value.DateTime, DateTimeKind.Utc),
                Body = pr.Body ?? string.Empty,
                MergeCommitSha = pr.MergeCommitSha ?? string.Empty,
                HeadSha = pr.Head.Sha,
                BaseSha = pr.Base.Sha,
                IsRollupPr = isRollupPr
            };

            pullRequestsDict[prNumber] = localPr;

            // Check rate limit periodically
            if (pullRequestsDict.Count % 50 == 0)
            {
                await CheckRateLimitAsync();
            }
        }
        
        // Final rate limit check
        await CheckRateLimitAsync();

        Console.WriteLine($"Total PRs (including rollup expansion): {pullRequestsDict.Count}");
        return pullRequestsDict.Values.OrderBy(pr => pr.MergedAt).ToList();
    }

    /// <summary>
    /// Determines if a PR is a rollup PR by checking title and body.
    /// </summary>
    private bool IsRollupPr(PullRequest pr)
    {
        // Check if title contains 'release' (case-insensitive)
        if (pr.Title != null && pr.Title.ToLower().Contains("release"))
            return true;

        // Check if body contains links to 2 or more other PRs from the same repo
        if (!string.IsNullOrEmpty(pr.Body))
        {
            var prNumbers = ExtractPrNumbersFromBody(pr.Body);
            if (prNumbers.Count >= 2)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Extracts PR numbers from text (looking for PR links like https://github.com/owner/repo/pull/123).
    /// </summary>
    private List<int> ExtractPrNumbersFromBody(string body)
    {
        var prNumbers = new List<int>();
        
        if (string.IsNullOrEmpty(body))
            return prNumbers;

        // Regex for PR links in this repo: https://github.com/{owner}/{repo}/pull/{number}
        var prLinkPattern = $@"https://github\.com/{System.Text.RegularExpressions.Regex.Escape(_owner)}/{System.Text.RegularExpressions.Regex.Escape(_repo)}/pull/(\d+)";
        var matches = System.Text.RegularExpressions.Regex.Matches(body, prLinkPattern);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out int prNumber))
            {
                if (!prNumbers.Contains(prNumber))
                {
                    prNumbers.Add(prNumber);
                }
            }
        }

        return prNumbers;
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
