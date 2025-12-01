# Quick Start Guide

## Prerequisites

1. **GitHub Personal Access Token**
   - Go to https://github.com/settings/tokens
   - Create a new token with `repo` scope
   - Save the token securely

2. **Google Service Account**
   - Go to Google Cloud Console
   - Create a service account
   - Enable Google Sheets API
   - Download JSON credentials
   - Share your spreadsheet with the service account email (found in the JSON)

3. **Local Git Repository**
   - Clone your repository locally
   - Note the full path

## Setup

1. **Create Configuration Files**

   Copy the example files:
   ```powershell
   Copy-Item projectGroups.example.json projectGroups.json
   Copy-Item teams.example.json teams.json
   ```

2. **Edit projectGroups.json**
   
   Add your project group prefixes:
   ```json
   [
     "Utilities",
     "Infrastructure.Core"
   ]
   ```

3. **Edit teams.json**
   
   Add your teams and GitHub usernames:
   ```json
   [
     {
       "teamName": "Team Alpha",
       "authors": ["githubuser1", "githubuser2"]
     }
   ]
   ```

4. **Create Google Spreadsheet**
   - Create a new Google Spreadsheet
   - Note the Spreadsheet ID (from the URL)
   - Create three sheets named: `ProjectGroups`, `PRs`, `Teams`
   - Share with service account email

## Running the Tool

```powershell
dotnet run --project src/ProjectInsights -- `
  --start-date 2025-10-01 `
  --end-date 2025-10-31 `
  --project-groups projectGroups.json `
  --teams teams.json `
  --github-pat ghp_YourGitHubToken `
  --google-creds path/to/service-account.json `
  --spreadsheet-id 1ABC...XYZ `
  --repo-path C:/path/to/your/repo `
  --github-owner your-org-name `
  --github-repo your-repo-name
```

## Example Output

```
=== ProjectInsights ===
Date Range: 2025-10-01 to 2025-10-31
Repository: your-org/your-repo
Local Path: C:/path/to/repo

Loading configuration...
Loaded 15 project groups and 5 teams

Discovering .csproj files...
Found 312 .csproj files
Mapped to 18 unique project groups

Fetching PRs merged between 2025-10-01 and 2025-10-31...
GitHub API Rate Limit: 4989 requests remaining
Found 247 merged PRs in date range

Analyzing PR #12345: Feature update (1/247)
...

Exporting data to Google Sheets...
Writing ProjectGroups sheet...
Wrote 18 rows to ProjectGroups sheet
Writing PRs sheet...
Wrote 247 PRs to PRs sheet
Writing Teams sheet...
Wrote 18 rows to Teams sheet
Data export completed successfully

=== Analysis Complete ===
Analyzed 247 PRs across 18 project groups
```

## Troubleshooting

**"Rate limit too low to start"**
- Wait for GitHub API rate limit to reset (resets hourly)
- Check current limit at https://api.github.com/rate_limit

**"Project groups file not found"**
- Ensure projectGroups.json exists in the current directory
- Or provide full path: `--project-groups C:/full/path/to/projectGroups.json`

**"Duplicate authors found"**
- Check teams.json - each author can only be in one team
- Author matching is case-insensitive

**Google Sheets permission error**
- Open the service account JSON file
- Find the `client_email` field
- Share your spreadsheet with that email address (Editor access)

## Tips

- Run the tool monthly to track trends over time
- Use consistent date ranges (e.g., first day of month to last day)
- Keep projectGroups.json in version control to track grouping changes
- Store credentials securely (don't commit to git)
