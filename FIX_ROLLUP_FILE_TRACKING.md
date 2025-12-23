# Fix: Accurate File Tracking for PRs in Rollup Merges

## Problem

When PRs are merged as part of a rollup PR, using the `MergeCommitSha` to get file changes was inaccurate. The merge commit SHA represents the combined changes of all PRs merged together in the rollup, not just the individual PR's changes.

**Example:**
- PR #100, #101, #102 are all merged together in rollup PR #200
- When analyzing PR #100, using its `MergeCommitSha` would show files from PRs #101 and #102 as well
- This inflated file counts and made attribution incorrect

## Solution

Compare the PR's **head commit** (last commit on the PR branch) against its **base commit** (the point where the PR branched off) to get the actual changes made in that specific PR.

### Changes Made

#### 1. **LocalPullRequest Model** (`src/ProjectInsights/Models/LocalPullRequest.cs`)
- Added `HeadSha` property - the SHA of the PR's last commit
- Added `BaseSha` property - the SHA of the commit the PR was based on

```csharp
public string HeadSha { get; set; } = string.Empty;
public string BaseSha { get; set; } = string.Empty;
```

#### 2. **GitHubService** (`src/ProjectInsights/Services/GitHubService.cs`)
- Capture `HeadSha` and `BaseSha` when fetching PR details from GitHub API
- These values come from `pr.Head.Sha` and `pr.Base.Sha` respectively

```csharp
HeadSha = pr.Head.Sha,
BaseSha = pr.Base.Sha,
```

#### 3. **GitService** (`src/ProjectInsights/Services/GitService.cs`)
- Added new method `GetPullRequestFilesByHeadAndBase(string headSha, string baseSha)`
- This method compares the PR's head tree against its base tree
- Returns only the files that were actually changed in that specific PR

```csharp
var changes = repo.Diff.Compare<TreeChanges>(baseCommit.Tree, headCommit.Tree, compareOptions);
```

#### 4. **PrAnalysisService** (`src/ProjectInsights/Services/PrAnalysisService.cs`)
- Updated to use `GetPullRequestFilesByHeadAndBase()` instead of `GetPullRequestFiles()`
- Falls back to merge commit analysis if head/base SHAs are not available (safety)

```csharp
if (!string.IsNullOrEmpty(pr.HeadSha) && !string.IsNullOrEmpty(pr.BaseSha))
{
    files = _gitService.GetPullRequestFilesByHeadAndBase(pr.HeadSha, pr.BaseSha);
}
else
{
    files = _gitService.GetPullRequestFiles(pr.MergeCommitSha);
}
```

## How It Works

### Before (Inaccurate)
```
Rollup PR #200 merges PRs #100, #101, #102
MergeCommitSha of #200 = ABC123

For PR #100:
  GetPullRequestFiles(ABC123) compares ABC123 with its parent
  Result: Shows files from PRs #100 + #101 + #102 ?
```

### After (Accurate)
```
PR #100:
  HeadSha = DEF456 (last commit in PR #100's branch)
  BaseSha = GHI789 (commit where PR #100 branched from main)

For PR #100:
  GetPullRequestFilesByHeadAndBase(DEF456, GHI789)
  Result: Shows only files changed in PR #100 ?
```

## Benefits

1. **Accurate File Attribution** - Each PR now shows only its own files
2. **Correct Statistics** - LOC counts, file counts, and project group associations are now accurate
3. **Rollup PRs Handled Correctly** - Individual PRs within rollups are tracked separately
4. **Backwards Compatible** - Falls back to merge commit if head/base not available

## Testing Recommendations

Test with:
1. Regular PR merges (not in rollups) - should work same as before
2. PRs merged in rollups - should now show accurate file counts
3. Compare file counts before/after for a known rollup PR to verify accuracy

## No Additional API Calls

This fix uses data already available from the GitHub API (head and base SHAs), so there's no increase in API usage.
