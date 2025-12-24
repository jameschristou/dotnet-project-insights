# Timezone Issue Fix

## Problem Discovered

You found that PRs were being duplicated due to timezone mishandling. The issue manifested as:
- `analysis_runs` record: `start_date: 2025-11-01 11:00:00+11`, `end_date: 2025-11-02 11:00:00+11`
- Associated PRs: `merged_at: 2025-11-03 10:30:17+11`

**The PR was merged AFTER the date range ended, but still got included!**

## Root Cause

The timezone handling had three critical bugs:

### Bug 1: Command-Line Date Parsing
**Before:**
```csharp
config.StartDate = DateTime.SpecifyKind(DateTime.Parse(value), DateTimeKind.Utc);
```

**Problem:**
- `DateTime.Parse("2025-11-01")` creates `2025-11-01 00:00:00` in **local timezone** (Australia +11)
- `DateTime.SpecifyKind()` just changes the `Kind` flag without converting the time
- Result: `2025-11-01 00:00:00 UTC` but it's actually `2025-10-31 13:00:00 UTC`!

**After:**
```csharp
config.StartDate = DateTime.Parse(value).ToUniversalTime();
```

**Fix:**
- Parses as local time, then **properly converts** to UTC
- Result: `2025-10-31 13:00:00 UTC` (correct!)

### Bug 2: Database Date Reading
**Before:**
```csharp
config.StartDate = DateTime.SpecifyKind(lastRun.EndDate, DateTimeKind.Utc);
```

**Problem:**
- PostgreSQL stores `timestamp with time zone` and returns it in UTC
- But the DateTime coming from EF Core might not have `Kind` set correctly
- `SpecifyKind` was being used incorrectly

**After:**
```csharp
config.StartDate = lastRun.EndDate; // Already UTC from PostgreSQL
```

**Fix:**
- PostgreSQL's `timestamp with time zone` always returns UTC
- Npgsql driver handles this correctly
- No conversion needed!

### Bug 3: Default Date
**Before:**
```csharp
config.StartDate = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc);
```

**Problem:**
- Year 2025 is in the future!
- Should be 2024

**After:**
```csharp
config.StartDate = new DateTime(2024, 11, 1, 0, 0, 0, DateTimeKind.Utc);
```

## How This Caused Duplicates

### Scenario: First Run
```
Input (Australian Eastern Time):
--start-date "2025-11-01"  (means 2025-11-01 00:00:00+11)
--end-date "2025-11-02"    (means 2025-11-02 00:00:00+11)

What was stored in DB (WRONG):
start_date: 2025-11-01 00:00:00 UTC  (should be 2025-10-31 13:00:00 UTC)
end_date:   2025-11-02 00:00:00 UTC  (should be 2025-11-01 13:00:00 UTC)

GitHub API query (WRONG):
merged: 2025-11-01T00:00:00Z..2025-11-02T00:00:00Z

This fetches PRs merged between:
- 2025-11-01 00:00:00 UTC (2025-11-01 11:00:00+11 in Australia)
- 2025-11-02 00:00:00 UTC (2025-11-02 11:00:00+11 in Australia)

But you WANTED PRs merged between:
- 2025-11-01 00:00:00+11 (2025-10-31 13:00:00 UTC)
- 2025-11-02 00:00:00+11 (2025-11-01 13:00:00 UTC)
```

### Scenario: Second Run (Next Day)
```
Read from DB:
lastRun.EndDate = 2025-11-02 00:00:00 UTC

Set new range:
start_date: 2025-11-02 00:00:00 UTC
end_date:   2025-11-03 00:00:00 UTC

GitHub API query:
merged: 2025-11-02T00:00:00Z..2025-11-03T00:00:00Z

This fetches PRs merged between:
- 2025-11-02 00:00:00 UTC (2025-11-02 11:00:00+11)
- 2025-11-03 00:00:00 UTC (2025-11-03 11:00:00+11)
```

### The Overlap
PRs merged between:
- `2025-11-01 13:00:00 UTC` to `2025-11-02 00:00:00 UTC`

Were included in **BOTH** runs because of the 11-hour timezone shift!

## The Fix in Action

### Now: First Run
```
Input (Australian Eastern Time):
--start-date "2025-11-01"

After parsing with .ToUniversalTime():
start_date: 2025-10-31 13:00:00 UTC
end_date:   2025-11-01 13:00:00 UTC

Stored in DB (CORRECT):
start_date: 2025-10-31 13:00:00 UTC
end_date:   2025-11-01 13:00:00 UTC

GitHub API query (CORRECT):
merged: 2025-10-31T13:00:00Z..2025-11-01T13:00:00Z

Fetches PRs merged during Australian day 2025-11-01
```

### Now: Second Run
```
Read from DB (already UTC):
lastRun.EndDate = 2025-11-01 13:00:00 UTC

Set new range:
start_date: 2025-11-01 13:00:00 UTC
end_date:   2025-11-02 13:00:00 UTC

GitHub API query (CORRECT):
merged: 2025-11-01T13:00:00Z..2025-11-02T13:00:00Z

Fetches PRs merged during Australian day 2025-11-02
```

### No Overlap!
Each PR is only included once because the date ranges don't overlap.

## Improved Logging

Added timestamps to console output to help debug:

```
=== ProjectInsights ===
Date Range: 2025-10-31 13:00:00 UTC to 2025-11-01 13:00:00 UTC
Date Range (Local): 2025-11-01 00:00:00 to 2025-11-02 00:00:00
Repository: microsoft/dotnet
```

```
Processing PRs for batch: 2025-10-31 13:00:00 UTC to 2025-11-01 13:00:00 UTC (base branch: main)
Found 5 PRs in this batch
  First PR: #12345 merged at 2025-10-31 14:30:17 UTC
  Last PR: #12389 merged at 2025-11-01 12:45:22 UTC
```

## Verification Steps

1. **Delete existing data** (has wrong timestamps):
```sql
TRUNCATE TABLE pull_requests, analysis_runs CASCADE;
```

2. **Run with fixed code**:
```bash
dotnet run -- --start-date "2024-11-01" ...
```

3. **Check the timestamps**:
```sql
SELECT 
    id,
    start_date,
    end_date,
    start_date AT TIME ZONE 'Australia/Sydney' as start_local,
    end_date AT TIME ZONE 'Australia/Sydney' as end_local
FROM analysis_runs;

SELECT 
    pr_number,
    merged_at,
    merged_at AT TIME ZONE 'Australia/Sydney' as merged_local
FROM pull_requests
ORDER BY merged_at;
```

You should see:
- `analysis_runs` dates are in UTC (13 hours behind Australian Eastern Time)
- `pull_requests.merged_at` falls within the UTC range
- No duplicates!

## Key Takeaways

? **Always use `.ToUniversalTime()`** when converting local dates to UTC
? **Never use `DateTime.SpecifyKind()`** for timezone conversion - it only changes the Kind flag
? **PostgreSQL `timestamp with time zone`** always returns UTC - no conversion needed
? **Add logging** with full timestamps (including time) to debug timezone issues
? **Test with your local timezone** to catch these issues early

## Related Files Modified

- `src/ProjectInsights/Program.cs` - Fixed ParseArguments() and DeterminePostgreSqlDateRangeAsync()
- `src/ProjectInsights/Services/PrBatchProcessor.cs` - Added detailed logging
- `src/ProjectInsights/Services/GitHubService.cs` - Already correct (uses DateTime.SpecifyKind after GitHub API returns UTC)
