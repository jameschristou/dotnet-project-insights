namespace ProjectInsights.Models;

public class LocalPullRequestFile
{
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public int Changes { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}
