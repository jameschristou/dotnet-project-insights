# Incremental Analysis Feature

## Overview

The application now automatically tracks analysis progress and continues from where it left off when using PostgreSQL as the export destination. This eliminates the need to manually specify date ranges and ensures continuous, day-by-day analysis.

## Key Changes

### 1. Automatic Date Range Detection

**PostgreSQL Mode:**
- The application queries the `analysis_runs` table for the most recent run
- Uses the previous run's `end_date` as the new `start_date`
- Sets `end_date` to `start_date + 1 day`
- If no previous runs exist, starts from **2025-11-01** (default)

**Google Sheets Mode:**
- Continues to require `--start-date` and `--end-date` arguments
- No automatic date range detection

### 2. Transactional Integrity

All database operations for a single run now occur within **one transaction**:
- Creating the `analysis_run` record
- Inserting all `pull_requests` records
- Inserting all `pr_files` records
- Inserting all `pr_projects` records
- Upserting `daily_project_stats` records
- Upserting `daily_team_project_stats` records

**Benefit:** If any operation fails, the entire transaction is rolled back. The `analysis_runs` table will only show dates for which all data has been successfully written.

### 3. Command-Line Argument Changes

**Before:**
```powershell
# Both modes required dates
--start-date 2025-11-01
--end-date 2025-11-02
```

**After:**
```powershell
# PostgreSQL mode - dates are automatic (omit these arguments)
--postgres-connection "connection_string"

# Google Sheets mode - dates still required
--start-date 2025-11-01
--end-date 2025-11-02
--google-creds path/to/creds.json
--spreadsheet-id spreadsheet_id
```

## Implementation Details

### New Method: `DeterminePostgreSqlDateRangeAsync`

```csharp
static async Task DeterminePostgreSqlDateRangeAsync(Models.AppConfig config)
{
    // Queries database for last analysis run
    var lastRun = await unitOfWork.AnalysisRuns.GetLatestAsync(
        config.GitHubOwner, 
        config.GitHubRepo);
    
    if (lastRun != null)
    {
        // Continue from last run
        config.StartDate = lastRun.EndDate;
        config.EndDate = config.StartDate.AddDays(1);
    }
    else
    {
        // First run - use default
        config.StartDate = new DateTime(2025, 11, 1);
        config.EndDate = config.StartDate.AddDays(1);
    }
}
```

### Modified Method: `ExportToPostgreSqlAsync`

```csharp
static async Task ExportToPostgreSqlAsync(...)
{
    using var dbContext = new ProjectInsightsDbContext(...);
    using var unitOfWork = new UnitOfWork(dbContext);
    
    try
    {
        // Start transaction BEFORE any inserts
        await unitOfWork.BeginTransactionAsync();
        
        var dbExportService = new DatabaseExportService(unitOfWork, projectDiscovery);
        
        // All inserts/upserts happen within this transaction
        await dbExportService.ExportDataAsync(...);
        
        // Commit only if everything succeeded
        await unitOfWork.CommitTransactionAsync();
    }
    catch (Exception ex)
    {
        // Rollback on any error
        await unitOfWork.RollbackTransactionAsync();
        throw;
    }
}
```

### Modified Service: `DatabaseExportService.ExportDataAsync`

- Removed internal transaction management
- Now relies on caller-managed transaction
- This allows the entire export (including analysis_run creation) to be atomic

## Usage Examples

### First Run (PostgreSQL)
```powershell
dotnet run --project src/ProjectInsights -- `
  --project-groups projectGroups.json `
  --teams teams.json `
  --github-pat YOUR_PAT `
  --postgres-connection "Host=localhost;Database=projectinsights;..." `
  --repo-path C:/path/to/repo `
  --github-owner your-org `
  --github-repo your-repo
```

**Output:**
```
Determining date range from previous analysis runs...
No previous runs found - starting from default date 2025-11-01

=== ProjectInsights ===
Date Range: 2025-11-01 to 2025-11-02
Repository: your-org/your-repo
...
```

### Second Run (PostgreSQL)
```powershell
# Same command - no date arguments needed
dotnet run --project src/ProjectInsights -- `
  --project-groups projectGroups.json `
  --teams teams.json `
  --github-pat YOUR_PAT `
  --postgres-connection "Host=localhost;Database=projectinsights;..." `
  --repo-path C:/path/to/repo `
  --github-owner your-org `
  --github-repo your-repo
```

**Output:**
```
Determining date range from previous analysis runs...
Found previous run ending on 2025-11-02
Continuing from 2025-11-02 to 2025-11-03

=== ProjectInsights ===
Date Range: 2025-11-02 to 2025-11-03
Repository: your-org/your-repo
...
```

### Google Sheets Mode (Unchanged)
```powershell
dotnet run --project src/ProjectInsights -- `
  --start-date 2025-11-01 `
  --end-date 2025-11-02 `
  --project-groups projectGroups.json `
  --teams teams.json `
  --github-pat YOUR_PAT `
  --google-creds path/to/creds.json `
  --spreadsheet-id YOUR_SPREADSHEET_ID `
  --repo-path C:/path/to/repo `
  --github-owner your-org `
  --github-repo your-repo
```

## Benefits

1. **Automation:** No need to manually track which dates have been processed
2. **Consistency:** Each run processes exactly one day of data
3. **Data Integrity:** Transactional writes prevent partial data writes
4. **Reliability:** If a run fails, re-running will retry the same date
5. **Simplicity:** Fewer command-line arguments required

## Error Handling

### Scenario: PR Analysis Fails Midway

**Before:**
- Some data written to database
- `analysis_runs` table shows date processed
- Actual PR data incomplete
- Manual cleanup required

**After:**
- Entire transaction rolled back
- No data written to database
- `analysis_runs` table unchanged
- Re-running processes the same date again

### Scenario: Database Connection Lost During Insert

**Before:**
- Partial data in database
- Inconsistent state
- Manual recovery needed

**After:**
- Transaction automatically rolled back
- Database remains in consistent state
- Re-run to retry

## Migration Impact

- **Existing Users:** No breaking changes for Google Sheets mode
- **New PostgreSQL Users:** Benefit from automatic date tracking
- **Database Schema:** No changes required - uses existing `analysis_runs` table

## Technical Notes

- Default start date: **2025-11-01**
- Date increment: **1 day**
- Date format: `DateTime` (no timezone conversion applied)
- Transaction isolation: Default PostgreSQL isolation level
- Unique constraint: `analysis_runs` table allows duplicate dates per repo (successive runs can analyze same date)

## Future Enhancements

Potential improvements:
1. Add `--force-date` argument to override automatic date detection
2. Track failed runs separately
3. Add support for larger date ranges (e.g., weekly instead of daily)
4. Implement date range validation (prevent gaps in analysis)
5. Add resume capability for interrupted large analyses
