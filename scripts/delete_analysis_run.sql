-- ============================================================================
-- Delete Analysis Run Script
-- ============================================================================
-- This script deletes all records related to a specific analysis_run.
-- 
-- Usage:
--   1. Replace <ANALYSIS_RUN_ID> with the actual ID you want to delete
--   2. Run the script in your PostgreSQL database
--
-- Note: The CASCADE delete will automatically remove related pull_requests,
-- pr_files, and pr_projects. However, daily stats tables need manual cleanup
-- if you want to remove aggregated data from that time period.
-- ============================================================================

-- Set the analysis run ID to delete
\set analysis_run_id <ANALYSIS_RUN_ID>

-- Start a transaction for safety
BEGIN;

-- Optional: View what will be deleted before proceeding
SELECT 
    'Analysis Run' as record_type,
    ar.id,
    ar.github_owner,
    ar.github_repo,
    ar.start_date,
    ar.end_date,
    ar.pr_count,
    ar.run_date
FROM analysis_runs ar
WHERE ar.id = :analysis_run_id;

SELECT 
    'Pull Requests to be deleted' as info,
    COUNT(*) as count
FROM pull_requests
WHERE analysis_run_id = :analysis_run_id;

SELECT 
    'PR Files to be deleted' as info,
    COUNT(*) as count
FROM pr_files pf
INNER JOIN pull_requests pr ON pf.pull_request_id = pr.id
WHERE pr.analysis_run_id = :analysis_run_id;

SELECT 
    'PR Projects to be deleted' as info,
    COUNT(*) as count
FROM pr_projects pp
INNER JOIN pull_requests pr ON pp.pull_request_id = pr.id
WHERE pr.analysis_run_id = :analysis_run_id;

-- Uncomment the following line to proceed with deletion
-- DELETE FROM analysis_runs WHERE id = :analysis_run_id;

-- If you're satisfied with the preview, commit the transaction
-- Otherwise, rollback
ROLLBACK;
-- COMMIT;

-- ============================================================================
-- Alternative: Delete specific analysis run directly (with confirmation)
-- ============================================================================
-- Replace <ANALYSIS_RUN_ID> below and uncomment to execute

/*
-- Delete analysis run (CASCADE will handle related records)
DELETE FROM analysis_runs 
WHERE id = <ANALYSIS_RUN_ID>
RETURNING 
    id, 
    github_owner, 
    github_repo, 
    start_date, 
    end_date, 
    pr_count,
    run_date;
*/

-- ============================================================================
-- Optional: Clean up related daily stats for the same date range
-- ============================================================================
-- Note: Only run this if you want to remove the aggregated stats as well.
-- This is NOT automatically handled by CASCADE delete since there's no FK.

/*
-- Option 1: Delete both tables in a single CTE
WITH run_info AS (
    SELECT start_date, end_date 
    FROM analysis_runs 
    WHERE id = <ANALYSIS_RUN_ID>
),
deleted_project_stats AS (
    DELETE FROM daily_project_stats
    WHERE day >= (SELECT DATE(start_date) FROM run_info)
      AND day < (SELECT DATE(end_date) FROM run_info)
    RETURNING *
)
DELETE FROM daily_team_project_stats
WHERE day >= (SELECT DATE(start_date) FROM run_info)
  AND day < (SELECT DATE(end_date) FROM run_info);
*/

/*
-- Option 2: Use subqueries instead of CTE
DELETE FROM daily_project_stats
WHERE day >= (SELECT DATE(start_date) FROM analysis_runs WHERE id = <ANALYSIS_RUN_ID>)
  AND day < (SELECT DATE(end_date) FROM analysis_runs WHERE id = <ANALYSIS_RUN_ID>);

DELETE FROM daily_team_project_stats
WHERE day >= (SELECT DATE(start_date) FROM analysis_runs WHERE id = <ANALYSIS_RUN_ID>)
  AND day < (SELECT DATE(end_date) FROM analysis_runs WHERE id = <ANALYSIS_RUN_ID>);
*/
