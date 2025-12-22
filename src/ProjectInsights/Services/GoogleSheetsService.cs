using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using ProjectInsights.Models;

namespace ProjectInsights.Services;

public class GoogleSheetsService
{
    private readonly SheetsService _sheetsService;
    private readonly string _spreadsheetId;

    public GoogleSheetsService(string credentialsPath, string spreadsheetId)
    {
        _spreadsheetId = spreadsheetId;

        GoogleCredential credential;
        using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(SheetsService.Scope.Spreadsheets);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        _sheetsService = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "ProjectInsights"
        });

        Console.WriteLine("Google Sheets service initialized");
    }

    public async Task ExportDataAsync(
        Dictionary<string, ProjectGroupStats> projectGroupStats,
        List<PrInfo> prInfos,
        Dictionary<string, Dictionary<string, int>> teamsMatrix,
        List<string> allProjectGroups,
        List<string> allTeams)
    {
        Console.WriteLine("Exporting data to Google Sheets...");

        // Clear and write ProjectGroups sheet
        await WriteProjectGroupsSheetAsync(projectGroupStats, allProjectGroups);

        // Clear and write PRs sheet
        await WritePrsSheetAsync(prInfos, allProjectGroups);

        // Clear and write Teams sheet
        await WriteTeamsSheetAsync(teamsMatrix, allProjectGroups, allTeams);

        Console.WriteLine("Data export completed successfully");
    }

    private async Task WriteProjectGroupsSheetAsync(
        Dictionary<string, ProjectGroupStats> stats,
        List<string> allProjectGroups)
    {
        Console.WriteLine("Writing ProjectGroups sheet...");

        var sheetName = "ProjectGroups";
        
        // Clear existing data
        await ClearSheetAsync(sheetName);

        // Prepare data
        var values = new List<IList<object>>
        {
            // Header row
            new List<object> { "Project Group", "PR Count", "Total LOC", "Files Modified", "Files Added" }
        };

        foreach (var projectGroup in allProjectGroups)
        {
            if (stats.TryGetValue(projectGroup, out var stat))
            {
                values.Add(new List<object>
                {
                    stat.ProjectGroupName,
                    stat.PrCount,
                    stat.TotalLinesChanged,
                    stat.FilesModified,
                    stat.FilesAdded
                });
            }
            else
            {
                values.Add(new List<object>
                {
                    projectGroup,
                    0,
                    0,
                    0,
                    0
                });
            }
        }

        await WriteToSheetAsync(sheetName, "A1", values);
        Console.WriteLine($"Wrote {values.Count - 1} rows to ProjectGroups sheet");
    }

    private async Task WritePrsSheetAsync(List<PrInfo> prInfos, List<string> allProjectGroups)
    {
        Console.WriteLine("Writing PRs sheet...");

        var sheetName = "PRs";
        
        // Clear existing data
        await ClearSheetAsync(sheetName);

        // Prepare header row
        var header = new List<object> { "PR Number", "Title", "Author", "Team", "Merged Date" };
        header.AddRange(allProjectGroups.Select(pg => (object)pg));

        var values = new List<IList<object>> { header };

        // Add PR rows
        foreach (var pr in prInfos.OrderBy(p => p.MergedAt))
        {
            var row = new List<object>
            {
                pr.Number,
                pr.Title,
                pr.Author,
                pr.Team,
                pr.MergedAt.ToString("yyyy-MM-dd")
            };

            // Add file counts for each project group
            //foreach (var projectGroup in allProjectGroups)
            //{
            //    var fileCount = pr.FileCountByProjectGroup.TryGetValue(projectGroup, out var count) ? count : 0;
            //    row.Add(fileCount);
            //}

            values.Add(row);
        }

        await WriteToSheetAsync(sheetName, "A1", values);
        Console.WriteLine($"Wrote {values.Count - 1} PRs to PRs sheet");
    }

    private async Task WriteTeamsSheetAsync(
        Dictionary<string, Dictionary<string, int>> teamsMatrix,
        List<string> allProjectGroups,
        List<string> allTeams)
    {
        Console.WriteLine("Writing Teams sheet...");

        var sheetName = "Teams";
        
        // Clear existing data
        await ClearSheetAsync(sheetName);

        // Prepare header row
        var header = new List<object> { "Project Group" };
        header.AddRange(allTeams.Select(t => (object)t));

        var values = new List<IList<object>> { header };

        // Add project group rows
        foreach (var projectGroup in allProjectGroups)
        {
            var row = new List<object> { projectGroup };

            if (teamsMatrix.TryGetValue(projectGroup, out var teamCounts))
            {
                foreach (var team in allTeams)
                {
                    var count = teamCounts.TryGetValue(team, out var c) ? c : 0;
                    row.Add(count);
                }
            }
            else
            {
                // Fill with zeros if no data
                foreach (var _ in allTeams)
                {
                    row.Add(0);
                }
            }

            values.Add(row);
        }

        await WriteToSheetAsync(sheetName, "A1", values);
        Console.WriteLine($"Wrote {values.Count - 1} rows to Teams sheet");
    }

    private async Task ClearSheetAsync(string sheetName)
    {
        var range = $"{sheetName}!A:ZZ";
        var requestBody = new ClearValuesRequest();

        var clearRequest = _sheetsService.Spreadsheets.Values.Clear(requestBody, _spreadsheetId, range);
        await clearRequest.ExecuteAsync();
    }

    private async Task WriteToSheetAsync(string sheetName, string startCell, List<IList<object>> values)
    {
        var range = $"{sheetName}!{startCell}";
        var valueRange = new ValueRange
        {
            Values = values
        };

        var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, range);
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

        await updateRequest.ExecuteAsync();
    }
}
