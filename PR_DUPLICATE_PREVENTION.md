# PR Duplicate Prevention Implementation

## Problem
PRs were being added multiple times into the `pull_requests` table when the same date range was analyzed multiple times.

## Solution
Added a unique constraint on the `pr_number` column to ensure each PR is only processed once globally across all analysis runs.

## Changes Made

### 1. Database Schema Changes
**File: `src\ProjectInsights\Data\Configurations\PullRequestConfiguration.cs`**
- Changed unique index from composite `(AnalysisRunId, PrNumber)` to just `PrNumber`
- This ensures each PR number can only exist once in the database

### 2. Repository Layer Changes
**File: `src\ProjectInsights\Repositories\Implementations\PullRequestRepository.cs`**
- Updated `BulkInsertPullRequestsAsync` to check for existing PRs before insertion
- Filters out duplicate PRs and only inserts new ones
- Provides console feedback about skipped duplicates

### 3. Service Layer Changes
**File: `src\ProjectInsights\Services\DatabaseExportService.cs`**
- Updated `SavePullRequestsAsync` to only insert files and projects for PRs that were actually inserted
- PRs that already exist in the database won't have their files/projects re-inserted

### 4. Database Migration
**Generated Migration: `20251223224142_AddUniquePrNumberConstraint.cs`**
- Drops the old composite unique index
- Creates a new unique index on `pr_number` column
- Creates a non-unique index on `analysis_run_id` for query performance

## How to Apply

### Option 1: Using Entity Framework Migrations (Recommended)
```bash
cd src\ProjectInsights
dotnet ef database update
```

### Option 2: Manual SQL Script
If you have existing duplicate PRs, use the SQL script at `scripts\add_unique_pr_number_constraint.sql`:

1. **First, check for existing duplicates:**
   ```sql
   SELECT pr_number, COUNT(*) as count
   FROM pull_requests
   GROUP BY pr_number
   HAVING COUNT(*) > 1
   ORDER BY count DESC;
   ```

2. **If duplicates exist, clean them up** (the script keeps the oldest record):
   ```sql
   DELETE FROM pull_requests
   WHERE id NOT IN (
       SELECT MIN(id)
       FROM pull_requests
       GROUP BY pr_number
   );
   ```

3. **Then apply the schema change:**
   ```bash
   psql -d your_database -f scripts/add_unique_pr_number_constraint.sql
   ```

## Behavior After Implementation

1. **First Analysis Run**: PRs are inserted normally
2. **Subsequent Runs with Overlapping Date Ranges**: 
   - Duplicate PRs are detected and skipped
   - Console output shows: "Skipping X duplicate PRs, inserting Y new PRs"
   - No files or projects are inserted for duplicate PRs
   - Transaction completes successfully

3. **Database Constraint**: If somehow a duplicate PR tries to be inserted, the database will reject it with a unique constraint violation

## Testing
To verify the fix is working:
1. Run an analysis for a date range
2. Run the same analysis again
3. Check the console output - you should see messages about skipping duplicate PRs
4. Verify in the database that PR numbers only appear once

## Rollback
If you need to rollback this change:
```bash
cd src\ProjectInsights
dotnet ef database update UpdatePrProjectUniqueConstraint
```

This will restore the original composite unique constraint on `(AnalysisRunId, PrNumber)`.
