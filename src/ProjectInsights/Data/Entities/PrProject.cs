namespace ProjectInsights.Data.Entities;

public class PrProject
{
    public long Id { get; set; }
    public long PullRequestId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectGroup { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public DateTime CreatedAt { get; set; }

    public PullRequest PullRequest { get; set; } = null!;
}
