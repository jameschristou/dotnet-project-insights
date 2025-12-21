namespace ProjectInsights.Data.Entities;

public class DailyProjectStats
{
    public long Id { get; set; }
    public DateOnly Day { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectGroup { get; set; } = string.Empty;
    public int PrCount { get; set; }
    public int TotalLinesChanged { get; set; }
    public int FilesModified { get; set; }
    public int FilesAdded { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
