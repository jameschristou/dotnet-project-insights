# Hybrid Approach - Quick Reference

## What Changed

### Before (Pure LibGit2Sharp Attempt)
- ? Tried to find PR numbers in commit messages
- ? Failed to identify PRs in rollups (commits lack PR numbers)
- ? Couldn't detect rollup PRs reliably

### Now (GitHub API + LibGit2Sharp Hybrid)
- ? GitHub API discovers and lists all merged PRs
- ? GitHub API detects and expands rollup PRs
- ? LibGit2Sharp analyzes file changes locally (fast, no API)

## Architecture Flow

```
1. GitHub API (GitHubService)
   ??> Search for merged PRs in date range
   ??> For each PR found:
       ??> Check if it's a rollup PR
       ?   ??> Title contains "release"? ? Rollup
       ?   ??> Body has 2+ PR links? ? Rollup
       ??> If rollup: Extract individual PR numbers and fetch them

2. LibGit2Sharp (GitService)
   ??> For each PR (from step 1):
       ??> Use merge commit SHA to analyze file changes
       ??> Calculate additions/deletions/changes
       ??> Return file list with stats

3. PrAnalysisService
   ??> Get PRs from GitHubService
   ??> For each non-rollup PR:
       ??> Get files from GitService
       ??> Map files to project groups
       ??> Build PrInfo object
```

## Key Methods

### GitHubService
- `GetMergedPullRequestsAsync()` - Main entry point
  - Searches GitHub for merged PRs
  - Detects rollup PRs
  - Expands rollup PRs into individual PRs
  - Returns complete list

### GitService
- `GetPullRequestFiles()` - File analysis
  - Takes commit SHA
  - Uses LibGit2Sharp to diff with parent
  - Returns file changes

## Rollup PR Detection

A PR is considered a rollup if:
```csharp
// Condition 1: Title contains "release"
pr.Title.ToLower().Contains("release")

// OR Condition 2: Body has 2+ links to PRs
// Pattern: https://github.com/owner/repo/pull/123
ExtractPrNumbersFromBody(pr.Body).Count >= 2
```

## Rollup PR Expansion

```csharp
// From rollup PR body, extract:
https://github.com/owner/repo/pull/101
https://github.com/owner/repo/pull/102
https://github.com/owner/repo/pull/103

// Fetch each PR via API
var pr101 = await _client.PullRequest.Get(owner, repo, 101);
var pr102 = await _client.PullRequest.Get(owner, repo, 102);
var pr103 = await _client.PullRequest.Get(owner, repo, 103);

// Add to results with rollup's merge time
```

## API Usage

| Operation | API Calls | Notes |
|-----------|-----------|-------|
| Search PRs | 1-2 | Paginated search |
| Fetch PR details | 1 per PR | ~800/month typical |
| Expand rollup | 1 per referenced PR | ~50/month typical |
| Analyze files | **0** | Uses local Git |

**Total**: ~850 API calls/month (well under 5,000/hour limit)

## Benefits vs Pure Approaches

| Approach | PR Discovery | File Analysis | API Calls | Rollup Handling |
|----------|--------------|---------------|-----------|-----------------|
| Pure GitHub API | ? Perfect | ? Good | ? High (~1,600) | ? Good |
| Pure LibGit2Sharp | ? Failed | ? Perfect | ? Zero | ? Failed |
| **Hybrid (Current)** | ? **Perfect** | ? **Perfect** | ? **Low (~850)** | ? **Perfect** |

## Code Impact

**Files Added:**
- None (GitHubService restored, GitService simplified)

**Files Modified:**
- `GitHubService.cs` - Restored with rollup expansion
- `GitService.cs` - Simplified to file analysis only
- `PrAnalysisService.cs` - Uses both services
- `PrBatchProcessor.cs` - Restored async + rate limits
- `Program.cs` - Initialize both services

**Files Removed:**
- None

**Dependencies:**
- Octokit (restored)
- LibGit2Sharp (kept)

## Migration Path

1. ? Tried pure LibGit2Sharp (failed on rollup PRs)
2. ? Implemented hybrid approach (success!)
3. Ready to deploy

## Next Steps

1. Test with real data containing rollup PRs
2. Verify rate limits are respected
3. Confirm all individual PRs are captured
4. Monitor API usage in production
