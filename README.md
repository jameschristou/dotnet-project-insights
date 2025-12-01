# ProjectInsights

A C# .NET 10 console application that analyzes Pull Requests in a large .NET solution, groups projects by configurable rules, tracks changes by team, and exports aggregated data to Google Sheets.

## Features

- **Project Grouping**: Configurable project groups using "starts with" matching (case-insensitive, longest-prefix-first)
- **Team Mapping**: Map git authors to teams with validation
- **Date Range Filtering**: Analyze PRs merged within a specific date range
- **GitHub API Integration**: Fetch PR data with rate limit tracking and safety thresholds
- **Google Sheets Export**: Export data to three sheets: ProjectGroups, PRs, and Teams
- **Smart Rate Limiting**: 
  - Exit if rate limit < 4000 on first request
  - Stop if rate limit ≤ 3000 during execution
  - Track remaining requests on every API call

## Requirements

- .NET 10 SDK
- GitHub Personal Access Token (PAT) with repository access
- Google Cloud Service Account with Sheets API access
- Local clone of the Git repository to analyze

## Installation

1. Clone this repository
2. Build the solution:
   ```powershell
   dotnet build
   ```

## Configuration

### 1. Project Groups (projectGroups.json)

Define project groups as an array of group names. Projects are matched using case-insensitive "starts with" logic, with longest prefix taking precedence.

Example:
```json
[
  "Infrastructure.Core",
  "Utilities"
]
```

### 2. Teams (teams.json)

Define teams with their members. Authors not in any team are assigned to "Unassigned".

Example:
```json
[
  {
    "teamName": "Team Alpha",
    "authors": ["john.doe", "jane.smith"]
  },
  {
    "teamName": "Team Beta",
    "authors": ["bob.jones", "alice.williams"]
  }
]
```

### 3. GitHub Personal Access Token

Create a PAT with `repo` scope at: https://github.com/settings/tokens

### 4. Google Service Account

1. Create a service account in Google Cloud Console
2. Enable Google Sheets API
3. Download the JSON credentials file
4. Share your target spreadsheet with the service account email

## Usage

```powershell
dotnet run --project src/ProjectInsights -- `
  --start-date 2025-10-01 `
  --end-date 2025-10-31 `
  --project-groups projectGroups.json `
  --teams teams.json `
  --github-pat YOUR_GITHUB_PAT `
  --google-creds path/to/service-account.json `
  --spreadsheet-id YOUR_SPREADSHEET_ID `
  --repo-path C:/path/to/local/repo `
  --github-owner your-org `
  --github-repo your-repo
```

### Command-Line Arguments

| Argument | Description | Required |
|----------|-------------|----------|
| `--start-date` | Start date for PR filter (yyyy-MM-dd) | Yes |
| `--end-date` | End date for PR filter (yyyy-MM-dd) | Yes |
| `--project-groups` | Path to projectGroups.json file | Yes |
| `--teams` | Path to teams.json file | Yes |
| `--github-pat` | GitHub Personal Access Token | Yes |
| `--google-creds` | Path to Google service account JSON | Yes |
| `--spreadsheet-id` | Target Google Spreadsheet ID | Yes |
| `--repo-path` | Path to local Git repository | Yes |
| `--github-owner` | GitHub repository owner | Yes |
| `--github-repo` | GitHub repository name | Yes |

## Output

The tool creates/updates three sheets in the specified Google Spreadsheet:

### 1. ProjectGroups Sheet

Columns:
- Project Group
- PR Count (number of PRs that modified the group)
- Total LOC (lines of code changed)
- Files Modified (count of modified/deleted/renamed files)
- Files Added (count of newly added files)

### 2. PRs Sheet

Columns:
- PR Number
- Title
- Author
- Team
- Merged Date
- One column per ProjectGroup showing file count

### 3. Teams Sheet

Matrix layout:
- Rows: ProjectGroups
- Columns: Teams
- Cells: Count of PRs from each team affecting each ProjectGroup

## Rate Limiting

The tool monitors GitHub API rate limits:
- **5,000 requests/hour** with authenticated access
- All PATs for the same account share this limit
- The tool checks rate limits on every request
- Exit codes:
  - `0` - Success
  - `1` - Error
  - `2` - Rate limit threshold exceeded

## Architecture

```
ProjectInsights/
├── Models/
│   ├── AppConfig.cs           # Application configuration
│   ├── Team.cs                # Team model
│   ├── ProjectGroupStats.cs   # Statistics per project group
│   └── PrInfo.cs              # Pull request information
├── Services/
│   ├── ConfigurationService.cs      # Load and validate configs
│   ├── ProjectDiscoveryService.cs   # Scan and map .csproj files
│   ├── GitHubService.cs             # GitHub API with rate limiting
│   ├── PrAnalysisService.cs         # Analyze PRs and aggregate data
│   ├── DataAggregationService.cs    # Build matrices and summaries
│   └── GoogleSheetsService.cs       # Export to Google Sheets
└── Program.cs                       # CLI entry point
```

## Technical Notes

- **Project Matching**: Uses longest-prefix-first matching to handle nested project structures
- **Whitespace Ignore**: File changes exclude whitespace-only modifications (using GitHub API stats)
- **Rebase Workflow**: Works with rebase-and-merge PR workflows by using GitHub API instead of Git merge commits
- **Data Refresh**: Each run clears existing sheet data and repopulates from scratch

## Troubleshooting

### "Rate limit too low to start"
Your GitHub account has < 4000 API requests remaining. Wait for the hourly window to reset.

### "Duplicate authors found across teams"
An author appears in multiple teams in `teams.json`. Each author must belong to only one team.

### "Project groups file is empty or invalid"
Check that `projectGroups.json` is valid JSON and contains at least one group name.

### Google Sheets permission error
Ensure the service account email has edit access to the target spreadsheet.

## License

This project is provided as-is for internal use.
