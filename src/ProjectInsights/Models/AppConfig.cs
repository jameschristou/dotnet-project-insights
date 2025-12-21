namespace ProjectInsights.Models;

public class AppConfig
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string ProjectGroupsPath { get; set; } = string.Empty;
    public string TeamsPath { get; set; } = string.Empty;
    public string GitHubPat { get; set; } = string.Empty;
    public string GoogleCredsPath { get; set; } = string.Empty;
    public string SpreadsheetId { get; set; } = string.Empty;
    public string PostgresConnectionString { get; set; } = string.Empty;
    public string RepoPath { get; set; } = string.Empty;
    public string GitHubOwner { get; set; } = string.Empty;
    public string GitHubRepo { get; set; } = string.Empty;
    public string GitHubBaseBranch { get; set; } = "main";
}
