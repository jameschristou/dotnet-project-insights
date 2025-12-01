using ProjectInsights.Models;

namespace ProjectInsights.Services;

public class DataAggregationService
{
    public Dictionary<string, Dictionary<string, int>> BuildTeamsMatrix(
        List<PrInfo> prInfos,
        List<string> allProjectGroups,
        List<Models.Team> teams)
    {
        // Matrix: ProjectGroup -> Team -> Count
        var matrix = new Dictionary<string, Dictionary<string, int>>();

        // Initialize matrix with all project groups and teams
        foreach (var projectGroup in allProjectGroups)
        {
            matrix[projectGroup] = new Dictionary<string, int>();
            foreach (var team in teams)
            {
                matrix[projectGroup][team.TeamName] = 0;
            }
            // Add Unassigned team
            matrix[projectGroup]["Unassigned"] = 0;
        }

        // Populate the matrix
        foreach (var prInfo in prInfos)
        {
            foreach (var projectGroup in prInfo.FileCountByProjectGroup.Keys)
            {
                if (!matrix.ContainsKey(projectGroup))
                {
                    matrix[projectGroup] = new Dictionary<string, int>();
                    foreach (var team in teams)
                    {
                        matrix[projectGroup][team.TeamName] = 0;
                    }
                    matrix[projectGroup]["Unassigned"] = 0;
                }

                if (!matrix[projectGroup].ContainsKey(prInfo.Team))
                {
                    matrix[projectGroup][prInfo.Team] = 0;
                }

                matrix[projectGroup][prInfo.Team]++;
            }
        }

        return matrix;
    }

    public List<string> GetAllTeamNames(List<Models.Team> teams)
    {
        var teamNames = teams.Select(t => t.TeamName).ToList();
        teamNames.Add("Unassigned");
        return teamNames.OrderBy(t => t).ToList();
    }
}
