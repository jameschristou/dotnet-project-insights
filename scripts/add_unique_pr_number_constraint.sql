-- Migration: Add unique constraint on pr_number column
-- This ensures each PR is only processed once across all analysis runs

-- Step 1: Drop the existing composite unique index on (analysis_run_id, pr_number)
DROP INDEX IF EXISTS ix_pull_requests_analysis_run_id_pr_number;

-- Step 2: Before adding the unique constraint, check for and remove any existing duplicates
-- This query shows you which PR numbers have duplicates
SELECT pr_number, COUNT(*) as count
FROM pull_requests
GROUP BY pr_number
HAVING COUNT(*) > 1
ORDER BY count DESC;

-- Step 3: If there are duplicates, you'll need to decide which ones to keep
-- This script keeps the oldest record (lowest id) for each pr_number
-- WARNING: Review the duplicates first before running this!
-- DELETE FROM pull_requests
-- WHERE id NOT IN (
--     SELECT MIN(id)
--     FROM pull_requests
--     GROUP BY pr_number
-- );

-- Step 4: Add the new unique constraint on pr_number alone
CREATE UNIQUE INDEX ix_pull_requests_pr_number ON pull_requests (pr_number);

-- Verification: Check that the constraint was created
SELECT
    indexname,
    indexdef
FROM
    pg_indexes
WHERE
    tablename = 'pull_requests'
ORDER BY
    indexname;
