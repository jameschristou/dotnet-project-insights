namespace ProjectInsights.Data.Entities;

public class PullRequest
{
    public long Id { get; set; }
    public long AnalysisRunId { get; set; }
    public int PrNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public DateTime MergedAt { get; set; }
    public string MergeCommitSha { get; set; } = string.Empty;
    public bool IsRollupPr { get; set; }
    public DateTime CreatedAt { get; set; }

    public AnalysisRun AnalysisRun { get; set; } = null!;
    public ICollection<PrFile> PrFiles { get; set; } = new List<PrFile>();
    public ICollection<PrProject> PrProjects { get; set; } = new List<PrProject>();
}
