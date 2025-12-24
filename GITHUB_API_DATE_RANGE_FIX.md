# GitHub Search API Date Range Issue and Fix

## Problem Discovered

PRs were being returned by the GitHub Search API that had merge dates **AFTER** the specified `endDate`. 

### Example
**Query:**
- `startDate: 2025-11-01 13:00:00 UTC`
- `endDate: 2025-11-02 13:00:00 UTC`

**Expected:** PRs merged between those exact times
**Actual:** PRs merged on `2025-11-03 10:30:17+11` (which is `2025-11-02 23:30:17 UTC`) were included!

## Root Cause

### GitHub Search API Behavior

The GitHub Search API's `merged:` filter has specific behavior:

1. **Date-only matching**: Ignores the time component of DateTime values
2. **Inclusive ranges**: Both start and end dates are fully included
3. **Full day inclusion**: Matches ANY PR merged during those calendar days

When you query:
```csharp
Merged = new DateRange(
    new DateTime(2025, 11, 1, 13, 0, 0, DateTimeKind.Utc),
    new DateTime(2025, 11, 2, 13, 0, 0, DateTimeKind.Utc)
)
```

GitHub interprets this as:
```
merged:2025-11-01..2025-11-02
```

Which means:
- **ALL PRs merged on 2025-11-01** (00:00:00 to 23:59:59 UTC)
- **ALL PRs merged on 2025-11-02** (00:00:00 to 23:59:59 UTC)

The time components `13:00:00` are **completely ignored**!

### Why This Caused Issues

When processing day-by-day:
```
Day 1: Query 2025-11-01 13:00 to 2025-11-02 13:00
       GitHub returns PRs from 2025-11-01 00:00 to 2025-11-02 23:59
       Includes PRs merged on 2025-11-02 between 13:00 and 23:59

Day 2: Query 2025-11-02 13:00 to 2025-11-03 13:00
       GitHub returns PRs from 2025-11-02 00:00 to 2025-11-03 23:59
       Includes PRs merged on 2025-11-02 between 00:00 and 23:59
```

**Result:** PRs merged on 2025-11-02 between 13:00 and 23:59 appeared in **BOTH** queries!

This, combined with the timezone issue, was causing duplicate PRs.

## The Fix

### Solution: Post-Query Filtering

Since GitHub's API doesn't support time-based filtering, we:

1. **Query a date range** (GitHub uses dates only)
2. **Retrieve full PR details** (includes precise `merged_at` timestamp)
3. **Filter results** by exact timestamp in application code

### Implementation

```csharp
public async Task<List<LocalPullRequest>> GetMergedPullRequestsAsync(
    DateTime startDate, 
    DateTime endDate, 
    string baseBranch)
{
    // GitHub API ignores time, so we query by date only
    var searchStartDate = startDate.Date;  // Start of day
    var searchEndDate = endDate.Date;      // Start of day
    
    var searchRequest = new SearchIssuesRequest
    {
        // ... other fields ...
        Merged = new DateRange(searchStartDate, searchEndDate),
    };

    var searchResult = await _client.Search.SearchIssues(searchRequest);
    
    foreach (var prNumber in searchResult.Items.Select(i => i.Number))
    {
        var pr = await _client.PullRequest.Get(_owner, _repo, prNumber);
        
        var mergedAt = DateTime.SpecifyKind(pr.MergedAt.Value.DateTime, DateTimeKind.Utc);
        
        // CRITICAL: Filter by exact timestamp
        if (mergedAt < startDate || mergedAt >= endDate)
        {
            // Outside our time window, skip this PR
            continue;
        }
        
        // Process this PR...
    }
}
```

### Key Changes

1. **Search with date boundaries**:
   ```csharp
   var searchStartDate = startDate.Date; // 2025-11-01 00:00:00
   var searchEndDate = endDate.Date;     // 2025-11-02 00:00:00
   ```

2. **Filter with exact timestamps**:
   ```csharp
   if (mergedAt < startDate || mergedAt >= endDate)
       continue; // Skip this PR
   ```

3. **Better logging**:
   ```csharp
   Console.WriteLine($"GitHub API search range: {searchStartDate:yyyy-MM-dd} to {searchEndDate:yyyy-MM-dd}");
   Console.WriteLine($"  PR #{prNumber} merged at {mergedAt:yyyy-MM-dd HH:mm:ss} UTC - outside time range, skipping");
   Console.WriteLine($"Filtered out {filteredOutCount} PRs outside exact time range");
   ```

## Example Execution

### Query Parameters
```
startDate: 2025-11-01 13:00:00 UTC
endDate:   2025-11-02 13:00:00 UTC
```

### GitHub API Query
```
merged:2025-11-01..2025-11-02
```
Returns ~15 PRs merged on those two full days

### Post-Processing Filter
```
PR #12345 merged at 2025-11-01 12:30:00 UTC - outside time range, skipping
PR #12346 merged at 2025-11-01 14:15:00 UTC - within time range
PR #12347 merged at 2025-11-02 10:00:00 UTC - within time range
PR #12348 merged at 2025-11-02 14:00:00 UTC - outside time range, skipping
```

### Final Result
Only PRs with `13:00:00 ? mergedAt < 13:00:00 (next day)` are included

## Benefits of This Approach

? **Precise time filtering** - Only PRs in exact time window
? **No duplicates** - Each PR appears in exactly one batch
? **Clear logging** - Shows which PRs were filtered and why
? **Works with timezones** - Combined with timezone fix, handles all scenarios
? **No extra API calls** - Still need to fetch PR details anyway

## Performance Considerations

### API Request Count

For a 24-hour window:
- **Search API**: 1 request (returns list of PR numbers)
- **PR Details**: N requests (where N = number of PRs in date range)
- **Filtered out**: ~0-2 PRs per day (PRs merged outside the time window)

### Example
```
Date range: 2025-11-01 13:00 to 2025-11-02 13:00
GitHub returns: 10 PRs total
- 1 PR merged before 13:00 on 2025-11-01 ? filtered out
- 8 PRs merged within time window ? kept
- 1 PR merged after 13:00 on 2025-11-02 ? filtered out

API calls: 1 (search) + 10 (PR details) = 11 total
Final PRs: 8
```

### Rate Limit Impact
- Minimal - only 1-2 extra PR detail requests per day
- Well within the 5000/hour limit
- The filtered PRs were needed for the next/previous day anyway

## Alternative Approaches Considered

### ? Option 1: Use Git commit history
- **Pros**: Local, no API limits
- **Cons**: Doesn't work with rebase-and-merge workflow (no merge commits)

### ? Option 2: Multiple smaller time windows
- **Pros**: Could reduce filtered PRs
- **Cons**: More API calls, complex batching, still needs filtering

### ? Option 3: Current approach (date range + filter)
- **Pros**: Simple, accurate, minimal extra API calls
- **Cons**: Fetches ~1-2 extra PRs per day

## Testing the Fix

### Verification Steps

1. **Clear database**:
```sql
TRUNCATE TABLE pull_requests, analysis_runs CASCADE;
```

2. **Run with fixed code**:
```powershell
dotnet run -- --postgres-connection "..." (no dates needed)
```

3. **Check console output**:
```
GitHub API search range: 2024-11-01 to 2024-11-02 (will filter results to exact time range)
Found 10 merged PRs in GitHub search range
Getting PR #12345 using API
  PR #12345 merged at 2024-11-01 12:30:00 UTC - outside time range, skipping
Getting PR #12346 using API
  PR #12346 merged at 2024-11-01 14:15:00 UTC - within time range
...
Filtered out 2 PRs outside exact time range
Total PRs in exact time range: 8
```

4. **Verify no duplicates**:
```sql
SELECT pr_number, COUNT(*) as count
FROM pull_requests
GROUP BY pr_number
HAVING COUNT(*) > 1;
-- Should return 0 rows
```

5. **Verify date ranges**:
```sql
SELECT 
    ar.start_date,
    ar.end_date,
    MIN(pr.merged_at) as first_pr_merged,
    MAX(pr.merged_at) as last_pr_merged
FROM analysis_runs ar
JOIN pull_requests pr ON pr.analysis_run_id = ar.id
GROUP BY ar.id, ar.start_date, ar.end_date
ORDER BY ar.start_date;
```

Expected result:
```
start_date             | end_date               | first_pr_merged        | last_pr_merged
2024-11-01 13:00:00+00 | 2024-11-02 13:00:00+00 | 2024-11-01 13:15:00+00 | 2024-11-02 12:59:00+00
2024-11-02 13:00:00+00 | 2024-11-03 13:00:00+00 | 2024-11-02 13:00:00+00 | 2024-11-03 12:59:00+00
```

All `merged_at` values should be within their respective `[start_date, end_date)` ranges!

## Related Issues Fixed

This fix, combined with the timezone fix, resolves:
1. ? Duplicate PRs in database
2. ? PRs appearing in wrong analysis runs
3. ? PRs merged outside date range being included
4. ? Overlapping date ranges causing duplicates

## Documentation Updates

Updated files:
- `src/ProjectInsights/Services/GitHubService.cs` - Added post-query filtering
- `GITHUB_API_DATE_RANGE_FIX.md` - This document
- `TIMEZONE_FIX.md` - Companion document about timezone issues

## References

- [GitHub Search API Documentation](https://docs.github.com/en/rest/search#search-issues-and-pull-requests)
- [GitHub Search Syntax - Date Ranges](https://docs.github.com/en/search-github/searching-on-github/searching-issues-and-pull-requests#search-by-date)
- [Octokit.NET DateRange](https://octokitnet.readthedocs.io/en/latest/search/)
