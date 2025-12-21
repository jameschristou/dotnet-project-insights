namespace ProjectInsights.Data.Entities;

public class PrFile
{
    public long Id { get; set; }
    public long PullRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectGroup { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public int Changes { get; set; }
    public DateTime CreatedAt { get; set; }

    public PullRequest PullRequest { get; set; } = null!;
}
