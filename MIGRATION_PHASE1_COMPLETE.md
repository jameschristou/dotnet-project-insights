# PostgreSQL Migration - Phase 1 Complete ?

## What We've Accomplished

### 1. Added Required NuGet Packages
- ? `Npgsql.EntityFrameworkCore.PostgreSQL` (v10.0.0)
- ? `Microsoft.EntityFrameworkCore.Design` (v10.0.0)

### 2. Created Database Entity Classes
- ? `AnalysisRun` - Tracks each analysis execution
- ? `PullRequest` - Stores PR details
- ? `PrFile` - Files changed in each PR (with project_name and project_group)
- ? `PrProject` - Aggregated project info per PR
- ? `DailyProjectStats` - Daily aggregated stats per project
- ? `DailyTeamProjectStats` - Daily team × project matrix

### 3. Created Entity Configurations
- ? `AnalysisRunConfiguration`
- ? `PullRequestConfiguration`
- ? `PrFileConfiguration`
- ? `PrProjectConfiguration`
- ? `DailyProjectStatsConfiguration`
- ? `DailyTeamProjectStatsConfiguration`

### 4. Created DbContext
- ? `ProjectInsightsDbContext` with all DbSets
- ? `ProjectInsightsDbContextFactory` for design-time migrations

### 5. Updated Configuration
- ? Added `PostgresConnectionString` to `AppConfig`

### 6. Created Initial Migration
- ? Migration file: `20251220200501_InitialCreate.cs`

## Database Schema Summary

### Tables Created:
1. **analysis_runs** - Tracks each analysis run
2. **pull_requests** - Individual PRs analyzed
3. **pr_files** - Files changed (includes project_name & project_group)
4. **pr_projects** - Projects touched per PR
5. **daily_project_stats** - Daily aggregated project stats
6. **daily_team_project_stats** - Daily team × project stats

### Key Relationships:
- `pull_requests` ? `analysis_runs` (many-to-one)
- `pr_files` ? `pull_requests` (many-to-one)
- `pr_projects` ? `pull_requests` (many-to-one)

### Unique Constraints:
- `pull_requests`: (analysis_run_id, pr_number)
- `pr_projects`: (pull_request_id, project_name)
- `daily_project_stats`: (day, project_name)
- `daily_team_project_stats`: (day, project_name, team_name)

## Next Steps - Phase 2: Repository Pattern

Create repository interfaces and implementations:
1. `IAnalysisRunRepository`
2. `IPullRequestRepository`
3. `IDailyStatsRepository`
4. `IUnitOfWork`

## How to Apply Migration to PostgreSQL

### Prerequisites:
1. PostgreSQL installed and running locally
2. Database created: `CREATE DATABASE projectinsights;`

### Apply Migration:
```bash
cd src/ProjectInsights
dotnet ef database update
```

### Connection String Format:
```
Host=localhost;Port=5432;Database=projectinsights;Username=postgres;Password=yourpassword
```

## Testing the Setup

After applying the migration, verify tables were created:
```sql
\dt  -- List all tables in psql
```

Expected tables:
- analysis_runs
- pull_requests
- pr_files
- pr_projects
- daily_project_stats
- daily_team_project_stats
- __EFMigrationsHistory
