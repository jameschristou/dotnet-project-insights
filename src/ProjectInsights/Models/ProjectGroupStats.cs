namespace ProjectInsights.Models;

public class ProjectGroupStats
{
    public string ProjectGroupName { get; set; } = string.Empty;
    public int PrCount { get; set; }
    public int TotalLinesChanged { get; set; }
    public int FilesModified { get; set; }
    public int FilesAdded { get; set; }
}
