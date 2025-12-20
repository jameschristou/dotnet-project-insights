namespace ProjectInsights.Models;

public class LocalPullRequest
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime MergedAt { get; set; }
    public string Body { get; set; } = string.Empty;
    public string MergeCommitSha { get; set; } = string.Empty;
    public bool IsRollupPr { get; set; }
}
