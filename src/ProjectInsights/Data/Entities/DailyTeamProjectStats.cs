namespace ProjectInsights.Data.Entities;

public class DailyTeamProjectStats
{
    public long Id { get; set; }
    public DateOnly Day { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectGroup { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public int PrCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
