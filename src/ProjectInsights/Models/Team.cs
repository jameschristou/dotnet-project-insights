namespace ProjectInsights.Models;

public class Team
{
    public string TeamName { get; set; } = string.Empty;
    public List<string> Authors { get; set; } = new();
}
