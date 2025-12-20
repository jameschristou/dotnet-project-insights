# Hybrid Approach: GitHub API + LibGit2Sharp - Summary

## Overview
Successfully implemented a **hybrid approach** that uses GitHub API for PR discovery and rollup expansion, while using LibGit2Sharp for file analysis. This provides accurate PR identification while minimizing API calls and leveraging local Git data for detailed file analysis.

## Architecture

### GitHub API Usage (GitHubService)
- **Search for merged PRs** in date range and base branch
- **Detect rollup PRs** by checking:
  - Title contains "release" (case-insensitive)
  - PR body contains 2+ links to other PRs in the same repo
- **Expand rollup PRs** by fetching individual PRs referenced in the body
- **Rate limit management** (exit if < 4000, stop if ? 3000)

### LibGit2Sharp Usage (GitService)
- **Analyze file changes** for each PR using merge commit SHA
- Calculate additions, deletions, and total changes per file
- Handle file renames and status changes
- Work entirely with local repository (fast, no API calls)

## Key Components

### New/Updated Files

1. **`src\ProjectInsights\Services\GitHubService.cs`** (Restored)
   - Searches for merged PRs using GitHub Search API
   - Detects rollup PRs using title and body analysis
   - Extracts individual PR numbers from rollup PR bodies
   - Fetches details for all PRs (including individual PRs in rollups)
   - Returns `List<LocalPullRequest>` for consistency

2. **`src\ProjectInsights\Services\GitService.cs`** (Simplified)
   - Focuses only on file analysis using LibGit2Sharp
   - Takes a commit SHA and returns file changes
   - No PR discovery logic

3. **`src\ProjectInsights\Services\PrAnalysisService.cs`** (Updated)
   - Uses **both** GitHubService and GitService
   - Gets PR list from GitHub API
   - Gets file changes from LibGit2Sharp
   - Skips rollup PRs but processes their individual PRs

4. **`src\ProjectInsights\Services\PrBatchProcessor.cs`** (Restored)
   - Async processing with rate limit checks
   - Pauses if rate limit drops below threshold

5. **`src\ProjectInsights\Models\AppConfig.cs`** (Updated)
   - Restored `GitHubPat` property

6. **`src\ProjectInsights\Program.cs`** (Updated)
   - Initializes both GitHubService and GitService
   - Requires `--github-pat` argument again

## PR Filtering Logic

### What Works Perfectly
? **Direct PR merges** - Detected via GitHub API search
? **Rollup PR detection** - Checks title for "release" and body for 2+ PR links
? **Rollup PR expansion** - Fetches individual PRs referenced in rollup body
? **File analysis** - Uses local Git for fast, accurate file change detection

### Rollup PR Handling
When a rollup PR is detected:
1. Extract all PR numbers from the rollup PR body (looking for GitHub PR links)
2. Fetch each individual PR's details from GitHub API
3. Use the rollup's merge time for all individual PRs
4. Skip the rollup PR itself from analysis
5. Process each individual PR normally

## Benefits

1. **Accurate PR Discovery** - GitHub API knows exactly which PRs were merged
2. **Handles Rollup PRs** - Automatically expands rollup PRs into individual PRs
3. **Fast File Analysis** - LibGit2Sharp reads from local repo (no API calls)
4. **Minimal API Usage** - Only use API for PR discovery, not file analysis
5. **Rate Limit Safe** - Monitors and respects rate limits

## API Usage Estimate

For a typical month with 800 PRs:
- Search API: 1-2 requests (pagination)
- PR details: ~800 requests (one per PR)
- Rollup expansion: ~50 additional requests (for PRs in rollups)
- **Total: ~850 requests** (well within 5,000/hour limit)

File analysis for all PRs: **0 API requests** (uses local Git)

## Command-Line Usage

The `--github-pat` argument is now **required** again:

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

Fetching PRs merged between 2025-10-01 and 2025-10-31 into branch 'main'...
GitHub API Rate Limit: 4989 requests remaining
Found 247 merged PRs in date range and branch
Detected rollup PR #12500, extracting individual PRs...
  Extracted 8 individual PRs from rollup
Total PRs (including rollup expansion): 254

Processing PRs for batch: 2025-10-01 to 2025-10-02...
Analyzing PR #12345: Fix memory leak (1/254)
Analyzing PR #12346: Update dependencies (2/254)
Skipping rollup PR #12500: Release 1.0
...

=== Analysis Complete ===
Analyzed 254 PRs across 18 project groups
```

## Testing Recommendations

1. Test with a date range that includes:
   - Regular PR merges
   - Rollup PRs with release in title
   - Rollup PRs with PR links in body
   
2. Verify:
   - Rollup PRs are detected and skipped
   - Individual PRs from rollups are processed
   - File changes are accurate
   - Rate limits are respected

3. Monitor API usage during first run to ensure it stays within limits
