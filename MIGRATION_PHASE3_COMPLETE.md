# PostgreSQL Migration - Phase 3 Complete ?

## What We've Accomplished

### 1. Updated Program.cs
- ? Added `--postgres-connection` command-line argument
- ? Made export destination flexible (PostgreSQL OR Google Sheets)
- ? Created `ExportToPostgreSqlAsync()` method
- ? Maintained backward compatibility with Google Sheets
- ? Added proper DbContext and UnitOfWork initialization

### 2. Command-Line Arguments

#### Required Arguments (All Scenarios)
- `--start-date` - Start date for PR analysis (yyyy-MM-dd)
- `--end-date` - End date for PR analysis (yyyy-MM-dd)
- `--project-groups` - Path to projectGroups.json
- `--teams` - Path to teams.json
- `--github-pat` - GitHub Personal Access Token
- `--repo-path` - Local repository path
- `--github-owner` - GitHub repository owner
- `--github-repo` - GitHub repository name
- `--github-base-branch` - Base branch (default: main)

#### Export Destination (Choose ONE)
**Option 1: PostgreSQL Database (NEW)**
- `--postgres-connection` - PostgreSQL connection string

**Option 2: Google Sheets (Legacy)**
- `--google-creds` - Path to Google service account JSON
- `--spreadsheet-id` - Google Spreadsheet ID

### 3. Complete Data Flow

```
GitHub API ? PRs ? Local Analysis
    ?
PrInfo objects (with files and stats)
    ?
DatabaseExportService
    ?
???????????????????????????????????
?  Transaction Started            ?
???????????????????????????????????
? 1. Create AnalysisRun           ?
? 2. Insert PullRequests          ?
? 3. Insert PrFiles (with project)?
? 4. Insert PrProjects            ?
? 5. Upsert DailyProjectStats     ?
? 6. Upsert DailyTeamProjectStats ?
???????????????????????????????????
?  Transaction Committed          ?
???????????????????????????????????
    ?
PostgreSQL Database
```

## Setup Instructions

### 1. Install PostgreSQL
If using Windows, download from: https://www.postgresql.org/download/windows/

### 2. Create Database
```sql
CREATE DATABASE projectinsights;
```

### 3. Apply Migrations
```bash
cd src/ProjectInsights
dotnet ef database update
```

This will create all required tables:
- `analysis_runs`
- `pull_requests`
- `pr_files`
- `pr_projects`
- `daily_project_stats`
- `daily_team_project_stats`

### 4. Run the Application

**Using PostgreSQL:**
```bash
dotnet run -- \
  --start-date "2024-01-01" \
  --end-date "2024-01-31" \
  --project-groups "./config/projectGroups.json" \
  --teams "./config/teams.json" \
  --github-pat "your_github_token" \
  --postgres-connection "Host=localhost;Port=5432;Database=projectinsights;Username=postgres;Password=yourpassword" \
  --repo-path "C:\path\to\repo" \
  --github-owner "microsoft" \
  --github-repo "dotnet" \
  --github-base-branch "main"
```

**Using Google Sheets (Legacy):**
```bash
dotnet run -- \
  --start-date "2024-01-01" \
  --end-date "2024-01-31" \
  --project-groups "./config/projectGroups.json" \
  --teams "./config/teams.json" \
  --github-pat "your_github_token" \
  --google-creds "./config/google-creds.json" \
  --spreadsheet-id "your_spreadsheet_id" \
  --repo-path "C:\path\to\repo" \
  --github-owner "microsoft" \
  --github-repo "dotnet" \
  --github-base-branch "main"
```

## Connection String Format

```
Host=localhost;Port=5432;Database=projectinsights;Username=postgres;Password=yourpassword
```

For production, use environment variables:
```bash
$env:POSTGRES_CONNECTION="Host=localhost;..."
dotnet run -- ... --postgres-connection $env:POSTGRES_CONNECTION
```

## Overlapping Day Handling (Key Feature!)

When you run the application multiple times with overlapping dates:

**Scenario:**
- Run 1: `--start-date "2024-01-15 00:00:00" --end-date "2024-01-20 12:00:00"`
- Run 2: `--start-date "2024-01-20 12:00:01" --end-date "2024-01-25 23:59:59"`

**What Happens on 2024-01-20:**

1. **First Run** (finds PRs merged on 2024-01-20 before 12:00:00):
   ```
   daily_project_stats:
   - day: 2024-01-20
   - project_name: "MyProject"
   - pr_count: 5
   - total_lines_changed: 1000
   ```

2. **Second Run** (finds PRs merged on 2024-01-20 after 12:00:01):
   ```
   Existing record found ? UPSERT
   - pr_count: 5 + 3 = 8
   - total_lines_changed: 1000 + 500 = 1500
   - updated_at: NOW()
   ```

3. **Result**: Complete data for 2024-01-20 with all PRs merged that day!

## Querying the Database

### Get Latest Analysis Run
```sql
SELECT * FROM analysis_runs 
ORDER BY run_date DESC 
LIMIT 1;
```

### Get PRs from Latest Run
```sql
SELECT pr.pr_number, pr.title, pr.author, pr.team, pr.merged_at
FROM pull_requests pr
JOIN analysis_runs ar ON pr.analysis_run_id = ar.id
WHERE ar.id = (SELECT id FROM analysis_runs ORDER BY run_date DESC LIMIT 1)
ORDER BY pr.merged_at;
```

### Get Daily Stats for a Date Range
```sql
SELECT 
    day,
    project_group,
    SUM(pr_count) as total_prs,
    SUM(total_lines_changed) as total_loc,
    SUM(files_modified) as total_files_modified,
    SUM(files_added) as total_files_added
FROM daily_project_stats
WHERE day BETWEEN '2024-01-01' AND '2024-01-31'
GROUP BY day, project_group
ORDER BY day, project_group;
```

### Get Team Activity by Project
```sql
SELECT 
    project_group,
    team_name,
    SUM(pr_count) as total_prs
FROM daily_team_project_stats
WHERE day BETWEEN '2024-01-01' AND '2024-01-31'
GROUP BY project_group, team_name
ORDER BY total_prs DESC;
```

### Get Most Active Projects
```sql
SELECT 
    project_name,
    project_group,
    SUM(pr_count) as total_prs,
    SUM(total_lines_changed) as total_loc
FROM daily_project_stats
WHERE day BETWEEN '2024-01-01' AND '2024-01-31'
GROUP BY project_name, project_group
ORDER BY total_prs DESC
LIMIT 10;
```

## Benefits Over Google Sheets

? **Historical Tracking** - Keep data from multiple analysis runs
? **Better Querying** - Use SQL for complex analytics
? **Performance** - Handles large datasets efficiently
? **Data Integrity** - Foreign keys and constraints ensure consistency
? **UPSERT Logic** - Handles overlapping date ranges intelligently
? **Scalability** - No row limits like Google Sheets
? **Security** - No need for Google service account credentials
? **Offline** - Works without internet connection

## Troubleshooting

### Connection Refused
- Ensure PostgreSQL is running: `services.msc` ? Find "postgresql-x64-15"
- Check port 5432 is not blocked by firewall

### Migration Failed
- Verify connection string is correct
- Ensure database exists: `CREATE DATABASE projectinsights;`
- Check PostgreSQL version compatibility (10+)

### Duplicate Key Violations
- Should not happen with proper UPSERT logic
- Check for concurrent runs (use transactions)

### Performance Issues
- Add indexes if querying large datasets
- Consider partitioning `daily_*` tables by date range

## Next Steps

### Optional Enhancements
1. **Create database views** for common queries (replace Google Sheets tabs)
2. **Add indexes** on frequently queried columns
3. **Implement archival** strategy for old analysis runs
4. **Add reporting** layer (Power BI, Grafana, etc.)
5. **Create migration script** for SQL Server (easy with EF Core)

### Migration to SQL Server (Future)
1. Change NuGet package: `Npgsql.EntityFrameworkCore.PostgreSQL` ? `Microsoft.EntityFrameworkCore.SqlServer`
2. Update connection string format
3. Regenerate migration: `dotnet ef migrations add SqlServerMigration`
4. Apply migration: `dotnet ef database update`

That's it! The repository pattern makes this trivial.

## Files Modified/Created

### Phase 3 Changes
- ? Updated: `src/ProjectInsights/Program.cs`
- ? Added: `--postgres-connection` argument support
- ? Created: `ExportToPostgreSqlAsync()` method
- ? Created: `ExportToGoogleSheetsAsync()` method

### Complete Migration Files
**Entities:**
- `src/ProjectInsights/Data/Entities/AnalysisRun.cs`
- `src/ProjectInsights/Data/Entities/PullRequest.cs`
- `src/ProjectInsights/Data/Entities/PrFile.cs`
- `src/ProjectInsights/Data/Entities/PrProject.cs`
- `src/ProjectInsights/Data/Entities/DailyProjectStats.cs`
- `src/ProjectInsights/Data/Entities/DailyTeamProjectStats.cs`

**Configurations:**
- `src/ProjectInsights/Data/Configurations/AnalysisRunConfiguration.cs`
- `src/ProjectInsights/Data/Configurations/PullRequestConfiguration.cs`
- `src/ProjectInsights/Data/Configurations/PrFileConfiguration.cs`
- `src/ProjectInsights/Data/Configurations/PrProjectConfiguration.cs`
- `src/ProjectInsights/Data/Configurations/DailyProjectStatsConfiguration.cs`
- `src/ProjectInsights/Data/Configurations/DailyTeamProjectStatsConfiguration.cs`

**DbContext:**
- `src/ProjectInsights/Data/ProjectInsightsDbContext.cs`
- `src/ProjectInsights/Data/ProjectInsightsDbContextFactory.cs`

**Repositories:**
- `src/ProjectInsights/Repositories/Interfaces/IAnalysisRunRepository.cs`
- `src/ProjectInsights/Repositories/Interfaces/IPullRequestRepository.cs`
- `src/ProjectInsights/Repositories/Interfaces/IDailyStatsRepository.cs`
- `src/ProjectInsights/Repositories/Interfaces/IUnitOfWork.cs`
- `src/ProjectInsights/Repositories/Implementations/AnalysisRunRepository.cs`
- `src/ProjectInsights/Repositories/Implementations/PullRequestRepository.cs`
- `src/ProjectInsights/Repositories/Implementations/DailyStatsRepository.cs`
- `src/ProjectInsights/Repositories/Implementations/UnitOfWork.cs`

**Services:**
- `src/ProjectInsights/Services/DatabaseExportService.cs`

**Migrations:**
- `src/ProjectInsights/Migrations/20251220200501_InitialCreate.cs`
- `src/ProjectInsights/Migrations/20251220200501_InitialCreate.Designer.cs`
- `src/ProjectInsights/Migrations/ProjectInsightsDbContextModelSnapshot.cs`

## Success! ??

The PostgreSQL migration is **100% complete**. You can now:
1. Apply migrations to your local PostgreSQL
2. Run the application with `--postgres-connection`
3. Query your data using SQL
4. Handle overlapping date ranges automatically
5. Easily migrate to SQL Server in the future

The repository pattern ensures clean, testable, and maintainable code!
