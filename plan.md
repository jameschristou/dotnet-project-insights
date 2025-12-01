# ProjectInsights - Complete Plan

## Overview

Build a C# .NET 10 console application that analyzes PRs in a local Git repository clone, groups projects by configurable rules, tracks changes by team, and exports aggregated data to Google Sheets across three sheets (ProjectGroups, PRs, Teams).

## Key Requirements

### Requirement 1: Project Groups
- Group projects together using configurable JSON file
- Matching: case-insensitive "starts with" based on project name
- Example: ProjectGroup "Infrastructure" matches any project starting with "Infrastructure"
- Unmatched projects become their own individual ProjectGroup
- Use longest-prefix-first matching when multiple prefixes could match

### Requirement 2: Teams
- Git authors belong to Teams
- Teams configured via JSON file (team name + list of authors)
- Validate that authors are not listed in multiple teams
- Authors not in any team go to "Unassigned" team

### Requirement 3: Date Range
- Provide start and end date range to filter PRs by merge date

### Requirement 4: Data Refresh
- Clear existing data and repopulate sheets on each run

### Requirement 5: ProjectGroups Sheet
Rows = ProjectGroups, with columns:
- Number of PRs that modified files in the ProjectGroup
- Total LOC modified (excluding whitespace-only changes)
- Total files modified (excluding whitespace-only, including deletes/moves)
- Total files added

### Requirement 6: PRs Sheet
Rows = PRs, with columns showing:
- How many files were touched in each ProjectGroup per PR

### Requirement 7: Teams Sheet
Matrix layout:
- Rows = ProjectGroups
- Columns = Teams
- Cells = Number of PRs from that team modifying that ProjectGroup

## Architecture Decisions

### Git Repository Access
- Use local Git repository clone with hardcoded path (no checkout needed)
- Use `LibGit2Sharp` for Git operations

### Project Discovery
- Scan filesystem for all `.csproj` files
- Use directory-based mapping to determine which project a file belongs to

### Google Sheets Authentication
- Use service account with JSON key file

### LOC Calculation
- Use Git's `--ignore-all-space` equivalent to exclude whitespace-only changes

### PR Detection Strategy
- Use **GitHub API** to query PRs merged during date range
- Addresses rebase-and-merge workflow where individual PR commits appear directly in release branches without merge commits
- Use PAT (Personal Access Token) for authentication
- Rate limit: 5,000 requests per hour per account (shared across all PATs)
- Expected usage: ~800 PRs/month = 800-1,600 requests (well within limit)

### GitHub API Rate Limit Management
- Track `X-RateLimit-Remaining` header on every API request
- **First request**: If `X-RateLimit-Remaining` < 4000, display message and exit immediately
- **Subsequent requests**: If `X-RateLimit-Remaining` ≤ 3000, display warning and stop application
- Parse rate limit headers from all GitHub API responses

## Implementation Steps

### Step 1: Create Solution Structure
- Create .NET 10 console application project
- Add NuGet packages:
  - `LibGit2Sharp` - Git operations
  - `Google.Apis.Sheets.v4` - Google Sheets API
  - `System.Text.Json` - JSON config parsing
  - `Octokit` or REST client for GitHub API

### Step 2: Implement Configuration System
- Create models for:
  - `projectGroups.json`: Array of { name, prefix }
  - `teams.json`: Array of { teamName, authors[] }
  - App settings: repository path, GitHub PAT, Google service account path, spreadsheet ID
- Implement JSON loaders with validation
- Validate no duplicate authors across teams
- Sort ProjectGroups by prefix length (longest first) for matching

### Step 3: Build Project Discovery
- Scan repository filesystem for all `.csproj` files
- Extract project names from file paths
- Map each project to ProjectGroup using longest-prefix-first "starts with" matching (case-insensitive)
- Projects without match become individual ProjectGroups
- Create file-path-to-ProjectGroup lookup based on directory hierarchy

### Step 4: Implement GitHub API Client
- Create client with PAT authentication
- Implement rate limit tracking:
  - Parse `X-RateLimit-Remaining` from response headers
  - Check first request for < 4000 (exit if true)
  - Check all requests for ≤ 3000 (stop if true)
  - Display appropriate messages on threshold violations
- Query PRs API filtered by:
  - Merged status
  - Date range (merge date between start and end)
- For each PR, fetch:
  - PR number, title, author
  - List of changed files
  - File change stats (additions, deletions)

### Step 5: Build PR Analysis Engine
- For each PR from GitHub API:
  - Map author to Team (or "Unassigned")
  - For each changed file, determine ProjectGroup via directory-based lookup
  - Use `LibGit2Sharp` to get detailed diff with `--ignore-all-space` equivalent
  - Calculate per file: LOC changes (excluding whitespace), additions, deletions, modifications
  - Aggregate data by ProjectGroup:
    - PR count per ProjectGroup
    - Total LOC modified per ProjectGroup
    - Files modified count (excluding whitespace-only changes)
    - Files added count

### Step 6: Build Data Aggregators
Create three datasets:

**ProjectGroups Sheet:**
- Rows = ProjectGroup names
- Columns: PR Count, Total LOC, Files Modified, Files Added

**PRs Sheet:**
- Rows = PR (number, title, author, date)
- Dynamic columns = One per ProjectGroup showing file count touched in that ProjectGroup

**Teams Sheet:**
- Rows = ProjectGroups
- Columns = Teams
- Cells = Count of PRs from that team touching that ProjectGroup

### Step 7: Implement Google Sheets Exporter
- Authenticate using service account JSON key file
- Clear existing data in all three sheets: "ProjectGroups", "PRs", "Teams"
- Write headers and data rows for each sheet
- Use batch requests for efficiency
- Handle API errors and rate limits (less critical than GitHub)

### Step 8: Add CLI Interface
Command-line arguments:
- `--start-date` (required): Start date for PR filter (format: yyyy-MM-dd)
- `--end-date` (required): End date for PR filter (format: yyyy-MM-dd)
- `--project-groups` (required): Path to `projectGroups.json`
- `--teams` (required): Path to `teams.json`
- `--github-pat` (required): GitHub Personal Access Token
- `--google-creds` (required): Path to Google service account JSON
- `--spreadsheet-id` (required): Target Google Spreadsheet ID
- `--repo-path` (optional): Repository path (can be hardcoded initially)

### Step 9: Error Handling & Logging
- Log rate limit status on each GitHub API request
- Exit codes: 0 (success), 1 (error), 2 (rate limit exceeded)
- Console output for progress tracking

## Configuration File Examples

### projectGroups.json
```json
[
  {
    "name": "Infrastructure.Core",
    "prefix": "Infrastructure.Core"
  },
  {
    "name": "Utilities.Services",
    "prefix": "Utilities.Services"
  }
]