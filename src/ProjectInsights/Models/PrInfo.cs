namespace ProjectInsights.Models;

public class PrInfo
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime MergedAt { get; set; }
    public string Team { get; set; } = string.Empty;
    public Dictionary<string, int> FileCountByProjectName { get; set; } = new();
    public List<LocalPullRequestFile> Files { get; set; } = new();
}
