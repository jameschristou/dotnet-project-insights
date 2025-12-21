namespace ProjectInsights.Data.Entities;

public class AnalysisRun
{
    public long Id { get; set; }
    public string GitHubOwner { get; set; } = string.Empty;
    public string GitHubRepo { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string BaseBranch { get; set; } = string.Empty;
    public DateTime RunDate { get; set; }
    public int PrCount { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<PullRequest> PullRequests { get; set; } = new List<PullRequest>();
}
