# How to Apply the Unique PR Number Constraint

## Problem
The migration is failing because duplicate PR numbers already exist in the database:
```
23505: could not create unique index "IX_pull_requests_pr_number"
```

## Solution Steps

### Step 1: Connect to your PostgreSQL database
```bash
psql -d your_database_name -U your_username
```

### Step 2: Identify duplicates
Run the first query from `scripts/cleanup_duplicate_prs.sql`:
```sql
SELECT 
    pr_number, 
    COUNT(*) as duplicate_count,
    STRING_AGG(id::text, ', ' ORDER BY id) as pr_ids,
    STRING_AGG(created_at::text, ', ' ORDER BY id) as created_dates
FROM pull_requests
GROUP BY pr_number
HAVING COUNT(*) > 1
ORDER BY duplicate_count DESC, pr_number;
```

This will show you:
- Which PR numbers are duplicated
- How many times each is duplicated
- The IDs of the duplicate records
- When they were created

### Step 3: Review the duplicates in detail
Run the second query to see full details:
```sql
SELECT 
    pr.id,
    pr.pr_number,
    pr.title,
    pr.author,
    pr.merged_at,
    pr.created_at,
    pr.analysis_run_id,
    ar.run_date as analysis_run_date,
    ar.start_date,
    ar.end_date
FROM pull_requests pr
JOIN analysis_runs ar ON pr.analysis_run_id = ar.id
WHERE pr.pr_number IN (
    SELECT pr_number
    FROM pull_requests
    GROUP BY pr_number
    HAVING COUNT(*) > 1
)
ORDER BY pr.pr_number, pr.id;
```

**Important:** Review this output to understand why duplicates exist. This will help identify the root cause.

### Step 4: Clean up duplicates
The cleanup script will keep the **oldest record** (lowest ID) for each PR number and delete the rest.

**?? WARNING: This will permanently delete data. Review carefully first!**

```sql
BEGIN;

-- Preview what will be deleted
SELECT 
    pr.id,
    pr.pr_number,
    pr.title,
    pr.created_at,
    'WILL BE DELETED' as action
FROM pull_requests pr
WHERE pr.id NOT IN (
    SELECT MIN(id)
    FROM pull_requests
    GROUP BY pr_number
)
ORDER BY pr.pr_number, pr.id;

-- If the preview looks correct, execute the delete
DELETE FROM pull_requests
WHERE id NOT IN (
    SELECT MIN(id)
    FROM pull_requests
    GROUP BY pr_number
);

COMMIT;
```

### Step 5: Verify cleanup
```sql
SELECT pr_number, COUNT(*) 
FROM pull_requests
GROUP BY pr_number
HAVING COUNT(*) > 1;
```

Should return **0 rows** if cleanup was successful.

### Step 6: Apply the migration
Now that duplicates are removed, apply the migration:
```bash
cd src\ProjectInsights
dotnet ef database update
```

### Step 7: Verify the constraint was created
```sql
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'pull_requests'
ORDER BY indexname;
```

You should see `IX_pull_requests_pr_number` with a unique index.

## Alternative: Use the all-in-one script
You can run the entire cleanup from the psql command line:
```bash
psql -d your_database_name -U your_username -f scripts/cleanup_duplicate_prs.sql
```

Then uncomment the DELETE section after reviewing the duplicates.

## Next Steps
After applying the constraint:
1. Monitor your application logs for unique constraint violations
2. This will help identify the root cause of why duplicates are being created
3. The exception will include the PR number, making it easy to trace
