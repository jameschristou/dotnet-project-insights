using System.Text.Json;
using ProjectInsights.Models;

namespace ProjectInsights.Services;

public class ConfigurationService
{
    public List<string> LoadProjectGroups(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Project groups file not found: {path}");
        }

        var json = File.ReadAllText(path);
        var projectGroups = JsonSerializer.Deserialize<List<string>>(json);

        if (projectGroups == null || projectGroups.Count == 0)
        {
            throw new InvalidOperationException("Project groups file is empty or invalid");
        }

        // Sort by length descending for longest-prefix-first matching
        return projectGroups.OrderByDescending(pg => pg.Length).ToList();
    }

    public List<Team> LoadTeams(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Teams file not found: {path}");
        }

        var json = File.ReadAllText(path);
        var teams = JsonSerializer.Deserialize<List<Team>>(json);

        if (teams == null || teams.Count == 0)
        {
            throw new InvalidOperationException("Teams file is empty or invalid");
        }

        // Validate no duplicate authors
        var allAuthors = teams.SelectMany(t => t.Authors).ToList();
        var duplicates = allAuthors.GroupBy(a => a, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            throw new InvalidOperationException(
                $"Duplicate authors found across teams: {string.Join(", ", duplicates)}");
        }

        return teams;
    }

    public string GetTeamForAuthor(List<Team> teams, string author)
    {
        var team = teams.FirstOrDefault(t => 
            t.Authors.Any(a => a.Equals(author, StringComparison.OrdinalIgnoreCase)));
        
        return team?.TeamName ?? "Unassigned";
    }
}
